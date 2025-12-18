using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace ShipEnhancements.Models.Json;

public record ThemeDataJsonTheme(
    [JsonProperty("hullTexturePaths")] Dictionary<string, object> HullTexturePaths,
    [JsonProperty("woodTexturePaths")] Dictionary<string, object> WoodTexturePaths,
    [JsonProperty("glassMaterialPaths")] Dictionary<string, object> GlassMaterialPaths,
    [JsonProperty("lightThemes")] List<LightThemeDataJson> LightThemes,
    [JsonProperty("hullThemes")] List<HullThemeDataJson> HullThemes,
    [JsonProperty("thrusterThemes")] List<ThrusterThemeDataJson> ThrusterThemes,
    [JsonProperty("damageThemes")] List<DamageThemeDataJson> DamageThemes
);