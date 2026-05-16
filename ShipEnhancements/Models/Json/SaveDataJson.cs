using Newtonsoft.Json;

namespace ShipEnhancements.Models.Json;

public record SaveDataJson()
{
    // make sure version is always previous update here
    [JsonProperty("lastChangelogVersion")]
    public string LastChangelogVersion = "Release v2.3.1";
    [JsonProperty("learnedRadioCodes")]
    public int LearnedRadioCodes = 7;
}