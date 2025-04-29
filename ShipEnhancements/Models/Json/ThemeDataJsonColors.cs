using Newtonsoft.Json;
using UnityEngine;

namespace ShipEnhancements.Models.Json;

public record ThemeDataJsonColors(
    [JsonProperty("ship")] Color Ship,
    [JsonProperty("thrust")] Color Thrust,
    [JsonProperty("indicator")] Color Indicator,
    [JsonProperty("hullDmg")] Color HullDmg,
    [JsonProperty("componentDmg")] Color ComponentDmg,
    [JsonProperty("alarm")] Color Alarm,
    [JsonProperty("alarmLit")] Color AlarmLit,
    [JsonProperty("light")] Color Light
);