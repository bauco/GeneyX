using GeneyX;
using System.Collections.Generic;
using System.Linq;

public class PublicationRepository : IPublicationRepository
{
    private readonly List<Publication> _publications = new List<Publication>();

    public void AddPublications(IEnumerable<Publication> publications)
    {
        _publications.AddRange(publications);
    }

    public List<Publication> GetLatestPublications(int count)
    {
        return _publications.OrderByDescending(p => p.PublishedYear).Take(count).ToList();
    }

    public List<Publication> SearchPublications(string searchTerm)
    {
        return _publications
            .Where(p => p.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        p.Abstract.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
