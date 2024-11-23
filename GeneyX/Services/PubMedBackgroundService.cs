using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;
using System.Net;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Text;


namespace GeneyX.Services
{
    public class PubMedBackgroundService : IHostedService, IDisposable
    {
        private const string _ftpUrl = "ftp://ftp.ncbi.nlm.nih.gov/pubmed/updatefiles/";
        private readonly HashSet<string> _processedFiles = new HashSet<string>();
        private readonly PublicationRepository _repository;
        private readonly List<Publication> _publications = new();
        private readonly ILogger<PubMedBackgroundService> _logger;
        private Timer _timer;
        private readonly CrawlingConfiguration _crawlConfiguration;
        private static object _processingLock = new Object();

        public PubMedBackgroundService(PublicationRepository repository, ILogger<PubMedBackgroundService> logger, IOptions<CrawlingConfiguration> configuration)
        {
            _repository = repository;
            _logger = logger;
            _crawlConfiguration = configuration.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {

            _timer = new Timer(CrawlPubmed, null, TimeSpan.Zero, TimeSpan.FromMinutes(_crawlConfiguration.CrawlDurationMins));
            return;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private void CrawlPubmed(object state)
        {
            try
            {
                lock (_processingLock)
                {
                    CrawlRecentPublications();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PubMed crawling.");
            }
        }
        private async Task<List<string>> GetPMEDFiles()
        {
            List<string> files = new List<string>();
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_ftpUrl);

            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync();

            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.EndsWith(".gz"))
                    {
                        files.Add(line);
                    }
                }
            }

            _logger.LogInformation($"Directory List Complete, status {response.StatusDescription}");
            return files;
        }
        public async void CrawlRecentPublications()
        {
            _logger.LogInformation("Starting to crawl PubMed FTP...");
            try
            {
                List<string> files = await GetPMEDFiles();
                List<string> gzFileOrderd = files
                    .Select(file => new
                    {
                        FileName = file.Split(' ').Last(),
                        ModificationDate = GetModificationDate(file)
                    })
                    .Where(x => x.ModificationDate > _crawlConfiguration.StartCrawlingDate)
                    .OrderByDescending(x => x.ModificationDate)
                    .Select(x => x.FileName)
                    .ToList();
                foreach (string fileName in gzFileOrderd)
                {
                    int retryCount = 0;
                    while (!await HandleFile(fileName) && retryCount < 10)
                    {
                        retryCount++;
                    }
                    if(retryCount == 10)
                    {
                        _logger.LogWarning($"Failed to handle file {fileName}. Retrying... Attempt {retryCount}");
                        break;
                    }

                    if (_publications.Count >= 1000)
                    {
                        _repository.UpdatePublications(_publications);
                        break;
                    }
                }
                if(_publications.Count < 1000 && _publications.Count < 0)
                {
                    _repository.UpdatePublications(_publications);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PubMed crawling.");
            }
        }
        private async Task<bool> HandleFile(string fileName)
        {

            string? gzFile = await DownloadFileFromFTP(_ftpUrl + fileName);
            if (gzFile == null)
            {
                return false;
            }
            string? xmlContent = await DecompressGzipStream(gzFile);
            if (xmlContent != null)
            {
                ParsePublications(xmlContent);
                return true;
            }
            return false;
        }

        private async Task<string?> DownloadFileFromFTP(string ftpFileName)
        {

            try
            {

                string tempName = Path.GetTempFileName() + ".xml.gz";
                Console.WriteLine($"File {ftpFileName} start downloaded to {tempName}");
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(ftpFileName.Replace("ftp://", "https://"));
                    response.EnsureSuccessStatusCode();

                    await using (var fileStream = new FileStream(tempName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }

                Console.WriteLine($"File {ftpFileName} downloaded successfully to {tempName}");
                return tempName;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file.");
                return null;
            }
        }

        private async Task<string?> DecompressGzipStream(string filePath)
        {
            _logger.LogInformation("Start decompress ");
            try
            {
                using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                StreamReader reader = new StreamReader(gzipStream, Encoding.UTF8);
                string? decompressGzip = await reader.ReadToEndAsync();
                fileStream.Close();
                gzipStream.Close();
                reader.Close();
                return decompressGzip;
            }
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex, "Failed to decompress file. It might not be a valid GZip file." + filePath);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
        static DateTime GetModificationDate(string fileLine)
        {
            string[] split = fileLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int year;
            string? month = split[5];
            string? day = split[6];
            if (split.Length > 7 && int.TryParse(split[7], out year))
            {
                return DateTime.Parse($"{month} {day} {year}");
            }
            else
            {
                year = DateTime.Now.Year; 
                return DateTime.Parse($"{month} {day} {year} {split[7]}");
            }
        }

        private void ParsePublications(string xmlContent)
        {
            try
            {
                XDocument? xmlDoc = XDocument.Parse(xmlContent);
                foreach (XElement article in xmlDoc.Descendants("PubmedArticle"))
                {
                    if (article != null)
                    {
                        Publication publication = new Publication
                        {
                            PMID = article.Descendants("PMID").FirstOrDefault()?.Value,
                            ArticleTitle = article.Descendants("ArticleTitle").FirstOrDefault()?.Value ?? "",
                            Abstract = article.Descendants("AbstractText").FirstOrDefault()?.Value ?? "",
                            PublishedYear = int.Parse(article.Descendants("PubMedPubDate")
                                .FirstOrDefault(p => (string)p.Attribute("PubStatus") == "pubmed")?
                                .Element("Year")?.Value ?? "0") // Default to 0 if not found
                        };
                        if (_publications.Count < 1000)
                        {
                            _publications.Add(publication);
                            _repository.AddPublication(publication);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }
    }
}

