using Newtonsoft.Json;

namespace ShipEnhancements.Models.Json;

public record RandomizerSettingsJson(
    [JsonProperty("inclusiveSettings")] string[] InclusiveSettings,
    [JsonProperty("exclusiveSettings")] string[] ExclusiveSettings
);
