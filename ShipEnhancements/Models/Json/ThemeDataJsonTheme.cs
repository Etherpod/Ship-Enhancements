using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ShipEnhancements.Models.Json;

public record ThemeDataJsonTheme(
    [JsonProperty("lightThemes")] LightThemeDataJson LightThemes,
    [JsonProperty("hullThemes")] HullThemeDataJson HullThemes,
    [JsonProperty("thrusterThemes")] ThrusterThemeDataJson ThrusterThemes,
    [JsonProperty("damageThemes")] DamageThemeDataJson DamageThemes
);