using GeneyX;

public class PublicationRepository : IPublicationRepository
{
    private readonly List<Publication> _publications = new List<Publication>();
    public void AddPublication(Publication publication)
    {
        if(!_publications.Any(p => p.PMID == publication.PMID && p.PublishedYear == publication.PublishedYear))
        {
            _publications.Add(publication);
        }
    }

    public void UpdatePublications(IEnumerable<Publication> publications)
    {
        _publications.Clear();
        _publications.AddRange(publications);
    }

    public List<Publication> GetLatestPublications(int count)
    {
        return _publications.OrderByDescending(p => p.PublishedYear).Take(count).ToList();
    }

    public List<Publication> SearchPublications(string searchTerm)
    {
        return _publications
            .Where(p => p.ArticleTitle.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        p.Abstract.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
