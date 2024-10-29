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
        private readonly IPublicationRepository _repository;
        private readonly List<Publication> _publications = new();
        private readonly ILogger<PubMedBackgroundService> _logger;
        private Timer _timer;
        private readonly CrawlingConfiguration _crawlConfiguration;
        private static object _processingLock = new Object();

        public PubMedBackgroundService(IPublicationRepository repository, ILogger<PubMedBackgroundService> logger, IOptions<CrawlingConfiguration> configuration)
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

            // Get the object used to communicate with the server.
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
                        _logger.LogInformation(line);
                        files.Add(line);
                    }
                }
            }

            _logger.LogInformation($"Directory List Complete, status {response.StatusDescription}");
            return files;
        }
        public async void CrawlRecentPublications()
        {
            // Get the list of gz files in the FTP directory
            _logger.LogInformation("Starting to crawl PubMed FTP...");
            try
            {
                List<string> files = await GetPMEDFiles();
                // Filter and sort the .gz files, keeping only their names
                List<string> gzFileOrderd = files
                    .Select(file => new
                    {
                        FileName = file.Split(' ').Last(), // Extract the file name
                        ModificationDate = GetModificationDate(file) // Get modification date
                    })
                    .Where(x => x.ModificationDate > _crawlConfiguration.StartCrawlingDate) // Filter by StartCrawlingDate
                    .OrderByDescending(x => x.ModificationDate) // Sort by modification date descending
                    .Select(x => x.FileName) // Return only the file name
                    .ToList();
                foreach (string fileName in gzFileOrderd)
                {
                    int retryCount = 0;
                    while (!await HandleFile(fileName) && retryCount < 3)
                    {
                        retryCount++;
                    }
                    if(retryCount == 3)
                    {
                        _logger.LogWarning($"Failed to handle file {fileName}. Retrying... Attempt {retryCount}");
                    }

                    if (_publications.Count >= 1000)
                    {
                        _repository.UpdatePublications(_publications);
                        break;
                    }
                }
                if(_publications.Count < 1000)
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

            byte[]? gzFile = await DownloadFileFromFTP(_ftpUrl + fileName);
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

        private async Task<byte[]?> DownloadFileFromFTP(string ftpFileName)
        {
            _logger.LogInformation("Start Download " + ftpFileName);

            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpFileName);
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                if (!reader.BaseStream.CanRead)
                {
                    _logger.LogError("Stream is closed. Cannot read data.");
                    return null;
                }
                MemoryStream memoryStream = new MemoryStream();
                await responseStream.CopyToAsync(memoryStream);
                byte[]? file = memoryStream.ToArray();

                _logger.LogInformation($"Download Complete, status {response.StatusDescription}");
                reader.Close();
                response.Close();
                memoryStream.Close();
                if (!IsGzipFile(file))
                {
                    _logger.LogError("The file is not a valid GZip format.");
                    return null;
                }
                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file.");
                return null;
            }
        }
        private bool IsGzipFile(byte[] fileData)
        {
            return fileData.Length > 2 && fileData[0] == 0x1F && fileData[1] == 0x8B;
        }

        private async Task<string?> DecompressGzipStream(byte[] compressedData)
        {
            _logger.LogInformation("Start decompress ");
            string tempFilePath = Path.GetTempFileName();
            try
            {
                // Write to a temporary file
                await File.WriteAllBytesAsync(tempFilePath, compressedData);
                using FileStream fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);
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
                _logger.LogError(ex, "Failed to decompress file. It might not be a valid GZip file.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
            finally
            {
                // Clean up the temporary file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        static DateTime GetModificationDate(string fileLine)
        {
            string[] split = fileLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            // Split the line and remove empty entries  Assuming the date can be in positions: Month [5], Day [6], Year [7] or Day (split[7]) with Year missing
            int year;
            string? month = split[5];
            string? day = split[6];

            // Check if the year is present (7h index) or assume it's last year
            if (split.Length > 7 && int.TryParse(split[7], out year))
            {
                // Year is present
                return DateTime.Parse($"{month} {day} {year}"); // Combine month, day, and year
            }
            else
            {
                // Year is not present, use current year or adjust logic if needed
                year = DateTime.Now.Year; // This assumes that the files are from the current year; adjust logic if needed
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
                        // Create a new Publication instance
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
                            _logger.LogInformation($"PMID {publication.PMID} Title {publication.ArticleTitle} publish {publication.PublishedYear}" );
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

