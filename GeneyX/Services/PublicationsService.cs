using GeneyX;

public class PublicationsService 
{
    private readonly PublicationRepository _repository;

    public PublicationsService(PublicationRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<Publication> GetLatestPublications(int count)
    {
        // Here you would call your crawler service and map to DTOs
        return _repository.GetLatestPublications(count)
                .Select(pub => new Publication
                {
                    PMID = pub.PMID,
                    ArticleTitle = pub.ArticleTitle,
                    Abstract = pub.Abstract,
                    PublishedYear = pub.PublishedYear
                });
    }

    public IEnumerable<Publication> SearchPublications(string term)
    {
        // Call your crawler service to search for publications by term
        return _repository.SearchPublications(term)
            .Select(pub => new Publication
            {
                PMID = pub.PMID,
                ArticleTitle = pub.ArticleTitle,
                Abstract = pub.Abstract,
                PublishedYear = pub.PublishedYear
            });
    }
}
