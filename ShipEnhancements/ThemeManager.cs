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
    }
    
    public Theme GetTheme(string name) => _nameToTheme[name];

    private IDictionary<string, Theme> LoadData(Stream resourceStream)
    {
        var data = JsonConvert.DeserializeObject<List<ThemeDataJsonTheme>>(
            new StreamReader(resourceStream).ReadToEnd()
        );

        if (data is null) {
            throw new JsonSerializationException("failed to parse hunt data ;-;");
        }

        return data
            .Select(Theme.From)
            .ToDictionary(
                theme => theme.Name,
                theme => theme
            );
    }
}

public record Theme(
    string Name,
    Color ShipColor,
    Color ThrustColor,
    Color IndicatorColor,
    Color HullDmgColor,
    Color ComponentDmgColor,
    Color AlarmColor,
    Color AlarmLitColor,
    Color Light
)
{
    internal static Theme From(ThemeDataJsonTheme themeData)
    {
        var colors = themeData.Colors;
        return new Theme(
            themeData.Name,
            colors.Ship,
            colors.Thrust,
            colors.Indicator,
            colors.HullDmg,
            colors.ComponentDmg,
            colors.Alarm,
            colors.AlarmLit,
            colors.Light
        );
    }
};