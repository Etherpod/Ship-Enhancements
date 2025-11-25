using System.Text.Json;
using System.Text.Json.Nodes;
using DitzyExtensions;
using DitzyExtensions.Collection;
using static ShipEnhancements.Settings.Generator.GenUtils;

namespace ShipEnhancements.Settings.Generator;

public static class SettingsGenerator
{
    private static readonly string GenTargetPath = "gen";
    private static readonly string SettingTargetFile = $"{GenTargetPath}/SESetting.g.cs";
    private static readonly string SettingsTargetFile = $"{GenTargetPath}/SESettings.g.cs";
    private static readonly string PresetTargetFile = $"{GenTargetPath}/SEPreset.g.cs";
    private static readonly IList<string> KeysToExclude = ["preset"];

    private static void Main(string[] args)
    {
        Directory.CreateDirectory(GenTargetPath);
        
        var settingData = ParseConfig();
        CreateSourceFiles(settingData);
        
        Console.WriteLine("Done!");
    }

    private static IList<SettingData> ParseConfig()
    {
        Console.WriteLine("Parsing config for settings...");

        var jsonText = File.ReadAllText("default-config.json");
        var config = JsonNode.Parse(jsonText)!;

        var settingData = config["settings"]!.AsObject()
            .Where(entry => !KeysToExclude.Contains(entry.Key.AsLower()))
            .Where(entry => entry.Value!["type"]!.ToString().AsLower() != "separator")
            .Select(entry =>
            {
                var settingData = new SettingData {
                    Name = entry.Key,
                    Title = entry.Value!["title"]!.ToString(),
                };
                
                var defaultValue = entry.Value!["value"]!.AsValue();
                switch (defaultValue.GetValueKind())
                {
                    case JsonValueKind.Number:
                        settingData.FieldType = "float";
                        settingData.DefaultValue = $"{defaultValue.Deserialize<float>()}f";
                        break;
                    case JsonValueKind.String:
                        settingData.FieldType = "string";
                        settingData.DefaultValue = S(defaultValue.Deserialize<string>()!);
                        break;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        settingData.FieldType = "bool";
                        settingData.DefaultValue = $"{defaultValue.Deserialize<bool>()}".AsLower();
                        break;
                }
                
                return settingData;
            })
            .AsList();

        return settingData;
    }

    private static void CreateSourceFiles(IList<SettingData> data)
    {
        Console.WriteLine("Creating SESetting file...");
        File.WriteAllText(SettingTargetFile, SESettingSource);
        Console.WriteLine("Creating SESettings file...");
        File.WriteAllText(SettingsTargetFile, CreateSettingsSource(data));
        Console.WriteLine("Creating SEPreset file...");
        File.WriteAllText(PresetTargetFile, CreatePresetSource(data));
    }
}