using System.Text.Json.Serialization;

namespace ErpSync.Application.DTOs;

public class SapODataResponseDto<T>
{
    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = new();
}
