using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ShipEnhancements.Models.Json;
using UnityEngine;

namespace ShipEnhancements;

public class ThemeManager
{
    private readonly IDictionary<string, Theme> _nameToTheme;

    public ThemeManager(string resourceName)
    {
        var resourceStream = typeof(ThemeManager).Assembly.GetManifestResourceStream(resourceName)!;
        
        _nameToTheme = LoadData(resourceStream);
        
        ShipEnhancements.WriteDebugMessage("theme manager initialized");
    }
    
    public Theme GetTheme(string name) => _nameToTheme[name];

    private IDictionary<string, Theme> LoadData(Stream resourceStream)
    {
        var data = JsonConvert.DeserializeObject<List<ThemeDataJsonTheme>>(
            new StreamReader(resourceStream).ReadToEnd()
        );

        if (data is null) {
            throw new JsonSerializationException("failed to parse data ;-;");
        }

        return data
            .Select(Theme.From)
            .ToDictionary(
                theme => theme.Name,
                theme => theme
            );
    }
}

public record LightTheme(
    string Name,
    Color LightColor
)
{
    internal static LightTheme From(LightThemeDataJson themeData)
    {
        return new LightTheme(
            themeData.Name,
            themeData.Light
        );
    }
};

public record HullTheme(
    string Name,
    Color HullColor
)
{
    internal static HullTheme From(HullThemeDataJson themeData)
    {
        return new HullTheme(
            themeData.Name,
            themeData.HullColor
        );
    }
};

public record ThrusterTheme(
    string Name,
    string ThrusterColor,
    float ThrusterIntensity,
    Color ThrusterLight,
    Color IndicatorColor,
    float IndicatorIntensity,
    Color IndicatorLight
)
{
    internal static ThrusterTheme From(ThrusterThemeDataJson themeData)
    {
        return new ThrusterTheme(
            themeData.Name,
            themeData.ThrusterColor,
            themeData.ThrusterIntensity,
            themeData.ThrusterLight,
            themeData.IndicatorColor,
            themeData.IndicatorIntensity,
            themeData.IndicatorLight
        );
    }
};

public record DamageTheme(
    string Name,
    Color HullColor,
    Color HullIntensity,
    Color CompColor,
    Color AlarmColor,
    Color AlarmLitColor,
    float AlarmLitIntensity,
    float IndicatorLight
)
{
    internal static DamageTheme From(DamageThemeDataJson themeData)
    {
        return new DamageTheme(
            themeData.Name,
            themeData.HullColor,
            themeData.HullIntensity,
            themeData.CompColor,
            themeData.AlarmColor,
            themeData.AlarmLitColor,
            themeData.AlarmLitIntensity,
            themeData.IndicatorLight
        );
    }
};