
using ApiAggregation.Models;
using ApiAggregation.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Controllers;

[ApiController]
[Route("api/countries")]
public class CountriesController : ControllerBase
{
    private readonly IApiAggregationService _aggregationService;

    public CountriesController(IApiAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    [HttpGet("all")]
    public async Task<ActionResult<AggregatedData>> GetWord() {
        try {
            var result = await _aggregationService.FetchFromApiAsync("countries", "/all");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("name/{name}")]
    public async Task<ActionResult<AggregatedData>> GetName(string name) {
        try {
            var result = await _aggregationService.FetchFromApiAsync("countries", $"/name/{name}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("alpha/{code}")]
    public async Task<ActionResult<AggregatedData>> GetCode(string code) {
        try {
            var result = await _aggregationService.FetchFromApiAsync("countries", $"/alpha/{code}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("currency/{currency}")]
    public async Task<ActionResult<AggregatedData>> GetCurrency(string currency) {
        try {
            var result = await _aggregationService.FetchFromApiAsync("countries", $"/countries/{currency}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("lang/{language}")]
    public async Task<ActionResult<AggregatedData>> GetLanguage(string language) {
        try {
            var result = await _aggregationService.FetchFromApiAsync("countries", $"/lang/{language}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("region/{region}")]
    public async Task<ActionResult<AggregatedData>> GetRegion(string region) {
        try {
            var result = await _aggregationService.FetchFromApiAsync("countries", $"/region/{region}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

