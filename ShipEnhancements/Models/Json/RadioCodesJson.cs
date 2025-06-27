using Newtonsoft.Json;

namespace ShipEnhancements.Models.Json;

public record RadioCodeJson(
    [JsonProperty("code")] int Code,
    [JsonProperty("filepath")] string Path
);
