using ApiAggregation.Models;
using ApiAggregation.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregation.Controllers;

[ApiController]
[Route("api/github")]
public class GitHubController : ControllerBase
{
    private readonly IApiAggregationService _aggregationService;

    public GitHubController(IApiAggregationService aggregationService)
    {
        _aggregationService = aggregationService;
    }

    [HttpGet("user/{username}")]
    public async Task<ActionResult<AggregatedData>> GetUser(string username)
    {
        try
        {
            var result = await _aggregationService.FetchFromApiAsync("github", $"/users/{username}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("repos/{owner}/{repo}")]
    public async Task<ActionResult<AggregatedData>> GetRepo(string owner, string repo)
    {
        try
        {
            var result = await _aggregationService.FetchFromApiAsync("github", $"/repos/{owner}/{repo}");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("users/{username}/repos")]
    public async Task<ActionResult<AggregatedData>> GetUserRepos(string username)
    {
        try
        {
            var result = await _aggregationService.FetchFromApiAsync("github", $"/users/{username}/repos");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
} 