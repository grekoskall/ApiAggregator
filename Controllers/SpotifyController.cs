using ApiAggregation.Models;
using ApiAggregation.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Controllers;

[ApiController]
[Route("api/spotify")]
public class SpotifyController : ControllerBase
{
    private readonly IApiAggregationService _aggregationService;
    private readonly SpotifyAuthService _authService;

    public SpotifyController(IApiAggregationService aggregationService, SpotifyAuthService authService)
    {
        _aggregationService = aggregationService;
        _authService = authService;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var state = Guid.NewGuid().ToString();
        var authUrl = _authService.GetAuthorizationUrl(state);
        return Redirect(authUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        try
        {
            var tokenResponse = await _authService.GetAccessTokenAsync(code);
            return Ok(new { 
                message = "Successfully authenticated with Spotify",
                access_token = tokenResponse.AccessToken,
                refresh_token = tokenResponse.RefreshToken,
                expires_in = tokenResponse.ExpiresIn
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("currently-playing")]
    public async Task<ActionResult<AggregatedData>> GetCurrentlyPlaying()
    {
        try
        {
            var result = await _aggregationService.FetchFromApiAsync("Spotify", "/me/player/currently-playing");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("playlists")]
    public async Task<ActionResult<AggregatedData>> GetPlaylists()
    {
        try
        {
            var result = await _aggregationService.FetchFromApiAsync("Spotify", "/me/playlists");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("top-tracks")]
    public async Task<ActionResult<AggregatedData>> GetTopTracks(
        [FromQuery] string timeRange = "medium_term",
        [FromQuery] int limit = 20)
    {
        try
        {
            var endpoint = $"/me/top/tracks?time_range={timeRange}&limit={limit}";
            var result = await _aggregationService.FetchFromApiAsync("Spotify", endpoint);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("top-artists")]
    public async Task<ActionResult<AggregatedData>> GetTopArtists(
        [FromQuery] string timeRange = "medium_term",
        [FromQuery] int limit = 20)
    {
        try
        {
            var endpoint = $"/me/top/artists?time_range={timeRange}&limit={limit}";
            var result = await _aggregationService.FetchFromApiAsync("Spotify", endpoint);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<AggregatedData>> Search(
        [FromQuery] string q,
        [FromQuery] string type = "track",
        [FromQuery] int limit = 20)
    {
        try
        {
            var endpoint = $"/search?q={Uri.EscapeDataString(q)}&type={type}&limit={limit}";
            var result = await _aggregationService.FetchFromApiAsync("Spotify", endpoint);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
} 