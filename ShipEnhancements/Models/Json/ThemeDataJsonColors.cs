using Newtonsoft.Json;
using UnityEngine;

namespace ShipEnhancements.Models.Json;

public record ThemeDataJsonColors(
    [JsonProperty("name")] string Name,
    [JsonProperty("light")] Color Light,
    [JsonProperty("ship")] Color Ship,
    [JsonProperty("thrust")] string ThrustName,
    [JsonProperty("thrustIntensity")] float ThrustIntensity,
    [JsonProperty("thrustLight")] Color ThrustLight,
    [JsonProperty("indicator")] Color Indicator,
    [JsonProperty("hullDmg")] Color HullDmg,
    [JsonProperty("hullDmgIntensity")] float HullDmgIntensity,
    [JsonProperty("componentDmg")] Color ComponentDmg,
    [JsonProperty("componentDmgIntensity")] float ComponentDmgIntensity,
    [JsonProperty("alarm")] Color Alarm,
    [JsonProperty("alarmLit")] Color AlarmLit,
    [JsonProperty("alarmLitIntensity")] float AlarmLitIntensity,
    [JsonProperty("dmgLight")] Color DmgLight
);

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
    [JsonProperty("hullIntensity")] Color HullIntensity,
    [JsonProperty("comp")] Color CompColor,
    [JsonProperty("alarm")] Color AlarmColor,
    [JsonProperty("alarmLit")] Color AlarmLitColor,
    [JsonProperty("alarmLitIntensity")] float AlarmLitIntensity,
    [JsonProperty("light")] float IndicatorLight
);