namespace ApiAggregation.Models;

public class SpotifyUserToken
{
    public string UserId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = string.Empty;
} 