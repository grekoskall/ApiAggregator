using System.Text.Json;

namespace ApiAggregation.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public JsonElement? RawData { get; set; }
} 