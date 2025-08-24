using Newtonsoft.Json;

namespace ShipEnhancements.Models.Json;

public record SaveDataJson()
{
    [JsonRequired]
    [JsonProperty("learnedRadioCodes")]
    public int LearnedRadioCodes;
}