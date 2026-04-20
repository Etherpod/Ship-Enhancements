using Newtonsoft.Json;

namespace ShipEnhancements.Models.Json;

public record SaveDataJson()
{
    // make sure version is always previous update here
    [JsonRequired]
    [JsonProperty("lastChangelogVersion")]
    public string LastChangelogVersion = "Release v2.2.2";
    [JsonRequired]
    [JsonProperty("learnedRadioCodes")]
    public int LearnedRadioCodes = 7;
}