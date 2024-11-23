using GeneyX;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
public class PublicationsController : ControllerBase
{
    private readonly PublicationsService _publicationService;

    public PublicationsController(PublicationsService publicationService)
    {
        _publicationService = publicationService;
    }

    [HttpGet("latest")]
    public IActionResult GetLatestPublications([FromQuery] int count = 1000)
    {
        IEnumerable<Publication> publications = _publicationService.GetLatestPublications(count);
        return Ok(publications);
    }

    [HttpGet("search")]
    public IActionResult SearchPublications([FromQuery] string term)
    {
        IEnumerable<Publication> results = _publicationService.SearchPublications(term);
        return Ok(results);
    }
}
