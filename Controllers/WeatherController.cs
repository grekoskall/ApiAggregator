using ApiAggregation.Models;
using ApiAggregation.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Controllers;

[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private readonly IApiAggregationService _aggregationService;

    public WeatherController(IApiAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    [HttpGet("city/{cityName}")]
    public async Task<ActionResult<AggregatedData>> GetByCity(string cityName)
    {
        try
        {
            var result = await _aggregationService.FetchFromApiAsync("OpenWeatherMap", $"/weather?q={cityName}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("coordinates")]
    public async Task<ActionResult<AggregatedData>> GetByCoordinates(
        [FromQuery] double lat, 
        [FromQuery] double lon)
    {
        try
        {
            var result = await _aggregationService.FetchFromApiAsync("OpenWeatherMap", $"/weather?lat={lat}&lon={lon}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("forecast/{cityName}")]
    public async Task<ActionResult<AggregatedData>> GetForecast(string cityName)
    {
        try
        {
            var result = await _aggregationService.FetchFromApiAsync("OpenWeatherMap", $"/forecast?q={cityName}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
} 