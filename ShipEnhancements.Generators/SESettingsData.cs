namespace ShipEnhancements.Generators;

internal record struct SESettingsData()
{
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } =  string.Empty;
    public EqArray<SESettingData> Settings { get; set; } = new();
}