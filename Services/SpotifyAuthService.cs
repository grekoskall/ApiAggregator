using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using ApiAggregation.Models;

namespace ApiAggregation.Services;

public class SpotifyAuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public SpotifyAuthService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _clientId = configuration["Spotify:ClientId"] ?? throw new ArgumentNullException("Spotify:ClientId");
        _clientSecret = configuration["Spotify:ClientSecret"] ?? throw new ArgumentNullException("Spotify:ClientSecret");
        _redirectUri = configuration["Spotify:RedirectUri"] ?? throw new ArgumentNullException("Spotify:RedirectUri");
    }

    public string GetAuthorizationUrl(string state)
    {
        var scopes = new[]
        {
            "user-read-private",
            "user-read-email",
            "user-read-currently-playing",
            "user-read-playback-state",
            "user-top-read",
            "user-read-recently-played",
            "playlist-read-private",
            "playlist-read-collaborative"
        };

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _clientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _redirectUri,
            ["state"] = state,
            ["scope"] = string.Join(" ", scopes)
        };

        var queryString = string.Join("&", queryParams.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
        return $"https://accounts.spotify.com/authorize?{queryString}";
    }

    public async Task<SpotifyTokenResponse> GetAccessTokenAsync(string code)
    {
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        var authString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", _redirectUri)
        });
        tokenRequest.Content = content;

        var response = await _httpClient.SendAsync(tokenRequest);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SpotifyTokenResponse>(responseContent)
            ?? throw new Exception("Failed to deserialize Spotify token response");
    }

    public async Task<SpotifyTokenResponse> RefreshTokenAsync(string refreshToken)
    {
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        var authString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
        tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });
        tokenRequest.Content = content;

        var response = await _httpClient.SendAsync(tokenRequest);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SpotifyTokenResponse>(responseContent)
            ?? throw new Exception("Failed to deserialize Spotify token response");
    }
} 