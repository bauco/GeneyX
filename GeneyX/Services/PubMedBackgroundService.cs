using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace GeneyX.Services
{
    public class PubMedBackgroundService : IHostedService, IDisposable
    {
        private readonly IPublicationRepository _repository;
        private readonly ILogger<PubMedBackgroundService> _logger;
        private Timer _timer;
        private readonly CrawlingOptions _crawlOptions;
        private readonly object _lock = new object();
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Single thread access

        public PubMedBackgroundService(IPublicationRepository repository, ILogger<PubMedBackgroundService> logger, IOptions<CrawlingOptions> options)
        {
            _repository = repository;
            _logger = logger;
            _crawlOptions = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(); // Wait until access is allowed
            _timer = new Timer(CrawlPubmed, null, TimeSpan.Zero, TimeSpan.FromMinutes(_crawlOptions.CrawlDurationMins));
            return;
        }

        private void CrawlPubmed(object state)
        {
            try
            {
                CrawlRecentPublications();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PubMed crawling.");
            }
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

        static DateTime GetModificationDate(string fileLine)
        {
            // This is a temporary method to retrieve modification date
            // You may need to adjust this to get the correct date from the actual file line
            var split = fileLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // Split the line and remove empty entries
                                                                                              // Assuming the date can be in positions: Month (split[11]), Day (split[12]), Year (split[13]) or Day (split[12]) with Year missing
            int year;
            var month = split[5];
            var day = split[6];

            // Check if the year is present (13th index) or assume it's last year
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
        // Async method for fetching data
        private readonly string _ftpUrl = "ftp://ftp.ncbi.nlm.nih.gov/pubmed/updatefiles/";
        private readonly List<Publication> _publications = new();

        public async void CrawlRecentPublications()
        {
            // Get the list of gz files in the FTP directory
            _logger.LogInformation("Starting to crawl PubMed FTP...");

            try
            {
                var files = await ExecuteCurlCommand();
                // Filter and sort the .gz files, keeping only their names
                var gzFileNames = files
                    .Where(file => file.EndsWith(".gz")) // Filter only .gz files
                    .Select(file => new
                    {
                        FileName = file.Split(' ').Last(), // Extract the file name
                        ModificationDate = GetModificationDate(file) // Get modification date
                    })
                    .Where(x => x.ModificationDate > _crawlOptions.StartCrawlingDate) // Filter by StartCrawlingDate
                    .OrderByDescending(x => x.ModificationDate) // Sort by modification date descending
                    .Select(x => x.FileName) // Return only the file name
                    .ToList();
                foreach (var fileName in gzFileNames)
                {
                    int retryCount = 0;
                    while (! await HandleFile(fileName) && retryCount < 3)
                    {
                        retryCount++;
                        _logger.LogWarning($"Failed to handle file {fileName}. Retrying... Attempt {retryCount}");
                    }
                    File.Delete(fileName);

                    if (_publications.Count >= 1000)
                    {
                        _repository.AddPublications(_publications);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PubMed crawling.");
            }
        }
        private async Task<bool> HandleFile(string fileName)
        {
            var tmpFile = await DownloadFileAsync(_ftpUrl + fileName, fileName);
            if (tmpFile == null)
            {
                _logger.LogError($"Failed to download file {fileName}");
                return false;
            }
            var xmlContent = await GetXmlContent(tmpFile);
            // Optionally, delete the temporary file
            if (xmlContent != null)
            {
                ParsePublications(xmlContent);
                return true;
            }
            return false;
        }
        private async Task<string?> GetXmlContent(string? tmpFile)
        {
            if (tmpFile == null)
            {
                return null;
            }
            try
            {
                // Extract the XML file from the gz file
                using (var fileStream = new FileStream(tmpFile, FileMode.Open, FileAccess.Read))
                using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                using (var reader = new StreamReader(gzipStream))
                {
                    return  await reader.ReadToEndAsync();
                }
            }
            catch (InvalidDataException ex)
            {
                _logger.LogError(ex, "Failed to decompress file. It might not be a valid GZip file.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return null;
        }
        public async Task<string> DownloadFileAsync(string url,string gzFileName)
        {
            try
            {
                var processInfo = new ProcessStartInfo("cmd.exe", "/c curl -s -S -O " + url)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        throw new InvalidOperationException("Failed to start the download process.");
                    }

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync();
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception(error);
                    }

                    // Ensure the file has been downloaded successfully
                    if (!File.Exists(gzFileName))
                    {
                        throw new FileNotFoundException($"Downloaded file not found: {gzFileName}");
                    }
                }
                return gzFileName;

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading {url}: {ex.Message}");
                throw;
            }
        }

    private async Task<string[]> ExecuteCurlCommand()
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/c curl -s -S " + _ftpUrl)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start the curl process.");
                }
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception(error);
                }

                return output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
        private void ParsePublications(string xmlContent)
        {
            try
            {
                var xmlDoc = XDocument.Parse(xmlContent);
                foreach (var article in xmlDoc.Descendants("PubmedArticle"))
                {
                    if (article != null)
                    {
                        // Create a new Publication instance
                        var publication = new Publication
                        {
                            PMID = article.Descendants("PMID").FirstOrDefault()?.Value,
                            Title = article.Descendants("ArticleTitle").FirstOrDefault()?.Value,
                            Abstract = article.Descendants("AbstractText").FirstOrDefault()?.Value,
                            PublishedYear = int.Parse(article.Descendants("PubMedPubDate")
                                .FirstOrDefault(p => (string)p.Attribute("PubStatus") == "pubmed")?
                                .Element("Year")?.Value ?? "0") // Default to 0 if not found
                        };
                        if (_publications.Count < 1000)
                        {
                            _publications.Add(publication);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);     
            }
        }

        public IEnumerable<Publication> GetLatestPublications(int count)
        {
            return _publications.OrderByDescending(p => p.PublishedYear).Take(count);
        }

        public IEnumerable<Publication> SearchPublications(string term)
        {
            return _publications
                .Where(p => p.Title.Contains(term, StringComparison.OrdinalIgnoreCase) || p.Abstract.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
    }
}

