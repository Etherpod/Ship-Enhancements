namespace ShipEnhancements.Generators;

internal record struct SESettingData()
{
    public string Name { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string SettingType { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
}