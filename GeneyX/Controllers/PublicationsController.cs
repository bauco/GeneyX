using GeneyX;
using GeneyX.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PublicationsController : ControllerBase
{
    private readonly IPublicationsService _publicationService;

    public PublicationsController(IPublicationsService publicationService)
    {
        _publicationService = publicationService;
    }

    [HttpGet("latest")]
    public IActionResult GetLatestPublications([FromQuery] int count = 1000)
    {
        var publications = _publicationService.GetLatestPublications(count);
        return Ok(publications);
    }

    [HttpGet("search")]
    public IActionResult SearchPublications([FromQuery] string term)
    {
        var results = _publicationService.SearchPublications(term);
        return Ok(results);
    }
}
