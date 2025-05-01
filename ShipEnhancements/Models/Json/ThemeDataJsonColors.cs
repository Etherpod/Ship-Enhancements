using Newtonsoft.Json;
using UnityEngine;

namespace ShipEnhancements.Models.Json;

public record LightThemeDataJson(
    [JsonProperty("name")] string Name,
    [JsonProperty("light")] Color Light
);

public record HullThemeDataJson(
    [JsonProperty("name")] string Name,
    [JsonProperty("hull")] Color HullColor
);

public record ThrusterThemeDataJson(
    [JsonProperty("name")] string Name,
    [JsonProperty("thruster")] string ThrusterColor,
    [JsonProperty("thrusterIntensity")] float ThrusterIntensity,
    [JsonProperty("thrusterLight")] Color ThrusterLight,
    [JsonProperty("indicator")] Color IndicatorColor,
    [JsonProperty("indicatorIntensity")] float IndicatorIntensity,
    [JsonProperty("indicatorLight")] Color IndicatorLight
);

public record DamageThemeDataJson(
    [JsonProperty("name")] string Name,
    [JsonProperty("hull")] Color HullColor,
    [JsonProperty("hullIntensity")] float HullIntensity,
    [JsonProperty("comp")] Color CompColor,
    [JsonProperty("compIntensity")] float CompIntensity,
    [JsonProperty("alarm")] Color AlarmColor,
    [JsonProperty("alarmLit")] Color AlarmLitColor,
    [JsonProperty("alarmLitIntensity")] float AlarmLitIntensity,
    [JsonProperty("light")] Color IndicatorLight
);