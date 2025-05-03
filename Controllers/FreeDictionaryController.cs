using ApiAggregation.Models;
using ApiAggregation.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Controllers;

[ApiController]
[Route("api/freedictionary")]
public class FreeDictionaryController : ControllerBase
{
    private readonly IApiAggregationService _aggregationService;

    public FreeDictionaryController(IApiAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    [HttpGet("{word}")]
    public async Task<ActionResult<AggregatedData>> GetWord(string word) {
        try {
            var result = await _aggregationService.FetchFromApiAsync("freedictionary", $"/{word}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
