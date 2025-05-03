using ApiAggregation.Models;
using ApiAggregation.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Controllers;

[ApiController]
[Route("api/news")]
public class NewsController : ControllerBase
{
    private readonly IApiAggregationService _aggregationService;

    public NewsController(IApiAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    [HttpGet("top-headlines")]
    public async Task<ActionResult<AggregatedData>> GetTopHeadlines(
        [FromQuery] string country = "us",
        [FromQuery] string category = null,
        [FromQuery] string q = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(country)) queryParams.Add($"country={country}");
            if (!string.IsNullOrEmpty(category)) queryParams.Add($"category={category}");
            if (!string.IsNullOrEmpty(q)) queryParams.Add($"q={q}");

            var queryString = string.Join("&", queryParams);
            var endpoint = $"/top-headlines{(queryParams.Any() ? "?" + queryString : "")}";

            var result = await _aggregationService.FetchFromApiAsync("NewsAPI", endpoint);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("everything")]
    public async Task<ActionResult<AggregatedData>> GetEverything(
        [FromQuery] string q,
        [FromQuery] string from = null,
        [FromQuery] string to = null,
        [FromQuery] string language = "en",
        [FromQuery] string sortBy = "publishedAt")
    {
        try
        {
            var queryParams = new List<string> { $"q={q}" };
            if (!string.IsNullOrEmpty(from)) queryParams.Add($"from={from}");
            if (!string.IsNullOrEmpty(to)) queryParams.Add($"to={to}");
            if (!string.IsNullOrEmpty(language)) queryParams.Add($"language={language}");
            if (!string.IsNullOrEmpty(sortBy)) queryParams.Add($"sortBy={sortBy}");

            var queryString = string.Join("&", queryParams);
            var endpoint = $"/everything?{queryString}";

            var result = await _aggregationService.FetchFromApiAsync("NewsAPI", endpoint);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("sources")]
    public async Task<ActionResult<AggregatedData>> GetSources(
        [FromQuery] string language = null,
        [FromQuery] string country = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(language)) queryParams.Add($"language={language}");
            if (!string.IsNullOrEmpty(country)) queryParams.Add($"country={country}");

            var queryString = string.Join("&", queryParams);
            var endpoint = $"/sources{(queryParams.Any() ? "?" + queryString : "")}";

            var result = await _aggregationService.FetchFromApiAsync("NewsAPI", endpoint);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
} 