using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ShipEnhancements.Models.Json;

public record ThemeDataJsonTheme(
    [JsonProperty("name")] string Name,
    [JsonProperty("colors")] ThemeDataJsonColors Colors
);