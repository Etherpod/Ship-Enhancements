using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ShipEnhancements.Models.Json;
using UnityEngine;

namespace ShipEnhancements;

public class ThemeManager
{
    private IDictionary<string, LightTheme> _nameToLightTheme;
    private IDictionary<string, HullTheme> _nameToHullTheme;
    private IDictionary<string, ThrusterTheme> _nameToThrusterTheme;
    private IDictionary<string, DamageTheme> _nameToDamageTheme;

    public ThemeManager(string resourceName)
    {
        var resourceStream = typeof(ThemeManager).Assembly.GetManifestResourceStream(resourceName)!;
        
        LoadData(resourceStream);
        
        ShipEnhancements.WriteDebugMessage("theme manager initialized");
    }
    
    public LightTheme GetLightTheme(string name) => _nameToLightTheme[name];
    public HullTheme GetHullTheme(string name) => _nameToHullTheme[name];
    public ThrusterTheme GetThrusterTheme(string name) => _nameToThrusterTheme[name];
    public DamageTheme GetDamageTheme(string name) => _nameToDamageTheme[name];

    private void LoadData(Stream resourceStream)
    {
        var data = JsonConvert.DeserializeObject<ThemeDataJsonTheme>(
            new StreamReader(resourceStream).ReadToEnd()
        );

        if (data is null) {
            throw new JsonSerializationException("failed to parse data ;-;");
        }

        _nameToLightTheme = data.LightThemes
            .Select(LightTheme.From)
            .ToDictionary(
                theme => theme.Name,
                theme => theme
            );
        _nameToHullTheme = data.HullThemes
            .Select(HullTheme.From)
            .ToDictionary(
                theme => theme.Name,
                theme => theme
            );
        _nameToThrusterTheme = data.ThrusterThemes
            .Select(ThrusterTheme.From)
            .ToDictionary(
                theme => theme.Name,
                theme => theme
            );
        _nameToDamageTheme = data.DamageThemes
            .Select(DamageTheme.From)
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
    float HullIntensity,
    Color CompColor,
    float CompIntensity,
    Color AlarmColor,
    Color AlarmLitColor,
    float AlarmLitIntensity,
    Color IndicatorLight
)
{
    internal static DamageTheme From(DamageThemeDataJson themeData)
    {
        return new DamageTheme(
            themeData.Name,
            themeData.HullColor,
            themeData.HullIntensity,
            themeData.CompColor,
            themeData.CompIntensity,
            themeData.AlarmColor,
            themeData.AlarmLitColor,
            themeData.AlarmLitIntensity,
            themeData.IndicatorLight
        );
    }
};