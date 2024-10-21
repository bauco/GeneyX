namespace GeneyX
{

    public interface IPublicationRepository
    {
        void AddPublications(IEnumerable<Publication> publications);
        List<Publication> GetLatestPublications(int count);
        List<Publication> SearchPublications(string searchTerm);
    }
    public interface IPublicationsService
    {
        IEnumerable<Publication> GetLatestPublications(int count);
        IEnumerable<Publication> SearchPublications(string term);
    }
    public class CrawlingOptions
    {
        // Define properties for the crawling configuration
        public DateTime StartCrawlingDate { get; set; }
        public int CrawlDurationMins { get; set; }
    }
    public class Publication
    {
        public string PMID { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Abstract { get; set; } = string.Empty;
        public int PublishedYear { get; set; }
    }
}
