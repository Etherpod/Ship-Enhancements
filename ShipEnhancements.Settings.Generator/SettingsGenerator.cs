using System.Runtime.InteropServices.ComTypes;
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
    private static readonly string PresetBaseTargetFile = $"{GenTargetPath}/SEPreset.g.cs";
    private static readonly Func<string,string> PresetTargetFile = presetName => $"{GenTargetPath}/{presetName}Preset.g.cs";

    private static void Main(string[] args)
    {
        // PresetInjector.InjectPresets();
        // return;
        
        Console.WriteLine("Clearing generated source directory...");
        if (Directory.Exists(GenTargetPath)) Directory.Delete(GenTargetPath, true);
        Directory.CreateDirectory(GenTargetPath);
        
        var settingData = ParseConfig();
        var presets = settingData
            .SelectMany(s =>
                s.PresetOverrides
                    .AsPairs()
                    .Select(po => (s.Name, po.key, po.value))
            )
            .GroupBy(po => po.key)
            .AsDict(
                group => group.Key,
                group => group.AsDict(po => po.Name, po => po.value)
            );
            
        CreateSourceFiles(settingData, presets);
        
        Console.WriteLine("Done!");
    }

    private static IList<SettingData> ParseConfig()
    {
        Console.WriteLine("Parsing config for settings...");

        var jsonText = File.ReadAllText("default-config.json");
        var config = JsonNode.Parse(jsonText)!;

        var settingData = config["settings"]!.AsObject()
            .Where(entry => entry.Value!["type"]!.ToString().AsLower() != "separator")
            .Select(entry =>
            {
                var settingData = new SettingData {
                    Name = entry.Key.PC(),
                    Title = entry.Value!["title"]!.ToString(),
                };
                
                var defaultValue = entry.Value!["value"]!.AsValue();
                var valueKind = defaultValue.GetValueKind();
                settingData.FieldType = valueKind switch
                {
                    JsonValueKind.Number => "float",
                    JsonValueKind.String => "string",
                    JsonValueKind.True or JsonValueKind.False => "bool",
                    _ => ""
                };
                settingData.DefaultValue = DeserializeSettingValue(valueKind, defaultValue);

                var presets = entry.Value!["presets"];
                if (presets != null)
                {
                    settingData.PresetOverrides["default"] = settingData.DefaultValue;
                    presets.AsObject()
                        .ForEachEntry((presetName, overrideVal) =>
                            settingData.PresetOverrides[presetName] =
                                DeserializeSettingValue(valueKind, overrideVal!.AsValue())
                        );
                }

                return settingData;
            })
            .AsList();

        return settingData;
    }

    private static string DeserializeSettingValue(JsonValueKind valueKind, JsonValue value) =>
        valueKind switch
        {
            JsonValueKind.Number => $"{value.Deserialize<float>()}f",
            JsonValueKind.String => value.Deserialize<string>()!.S(),
            JsonValueKind.True or JsonValueKind.False => $"{value.Deserialize<bool>()}".AsLower(),
            _ => ""
        };

    private static void CreateSourceFiles(
        IList<SettingData> data,
        IDictionary<string, IDictionary<string, string>> presets
    ) {
        Console.WriteLine("Creating SESetting file...");
        File.WriteAllText(SettingTargetFile, SESettingSource);
        
        Console.WriteLine("Creating SEPreset file...");
        File.WriteAllText(PresetBaseTargetFile, SEPresetSource);
        
        Console.WriteLine("Creating SESettings file...");
        File.WriteAllText(SettingsTargetFile, CreateSettingsSource(data));
        
        Console.WriteLine("Creating SpecificPreset files...");
        var settingDict = data.AsDict(s => s.Name);
        presets.ForEachEntry((presetName, overrideVals) =>
        {
            var targetFile = PresetTargetFile(presetName.PC());
            var presetSource = CreatePresetSource(settingDict, presetName.PC(), overrideVals);
            File.WriteAllText(targetFile, presetSource);
        });
    }
}