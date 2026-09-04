using System.Text.Json.Serialization;

namespace MockSap.Api.Models;

public class ODataCollectionResponse<T>
{
    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = new();
}
