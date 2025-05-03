using System.Net.Http.Headers;
using System.Text.Json;
using ApiAggregation.Models;
using Microsoft.Extensions.Configuration;

namespace ApiAggregation.Services;

public class SpotifyApiService : BaseApiService, IApiService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private string? _accessToken;
    private DateTime _tokenExpiration;

    public SpotifyApiService(IConfiguration configuration, HttpClient httpClient) 
        : base(httpClient, new SpotifyApiSettings())
    {
        _clientId = configuration["Spotify:ClientId"] ?? throw new ArgumentNullException("Spotify:ClientId");
        _clientSecret = configuration["Spotify:ClientSecret"] ?? throw new ArgumentNullException("Spotify:ClientSecret");
    }

    public override string GetApiName() => "Spotify";

    public async Task<ApiResponse> GetDataAsync()
    {
        await EnsureValidTokenAsync();

        var response = await _httpClient.GetAsync("https://api.spotify.com/v1/me/player/currently-playing");
        if (!response.IsSuccessStatusCode)
        {
            return new ApiResponse
            {
                Success = false,
                Message = $"Failed to get Spotify data: {response.StatusCode}"
            };
        }

        var content = await response.Content.ReadAsStringAsync();
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(content);
        
        return new ApiResponse
        {
            Success = true,
            Data = jsonElement,
            RawData = jsonElement,
            Message = "Successfully retrieved Spotify data"
        };
    }

    public async Task<ApiResponse> FetchDataAsync(string endpoint)
    {
        await EnsureValidTokenAsync();

        var response = await _httpClient.GetAsync($"https://api.spotify.com/v1{endpoint}");
        if (!response.IsSuccessStatusCode)
        {
            return new ApiResponse
            {
                Success = false,
                Message = $"Failed to get Spotify data: {response.StatusCode}"
            };
        }

        var content = await response.Content.ReadAsStringAsync();
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(content);
        
        return new ApiResponse
        {
            Success = true,
            Data = jsonElement,
            RawData = jsonElement,
            Message = "Successfully retrieved Spotify data"
        };
    }

    private async Task EnsureValidTokenAsync()
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiration)
        {
            return;
        }

        var tokenResponse = await GetAccessTokenAsync();
        _accessToken = tokenResponse.AccessToken;
        _tokenExpiration = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 60); 
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }

    private async Task<SpotifyTokenResponse> GetAccessTokenAsync()
    {
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        var authString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);
        
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });
        tokenRequest.Content = content;

        var response = await _httpClient.SendAsync(tokenRequest);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SpotifyTokenResponse>(responseContent) 
            ?? throw new Exception("Failed to deserialize Spotify token response");
    }
} 
