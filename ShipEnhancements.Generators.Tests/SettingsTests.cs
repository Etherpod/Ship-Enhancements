using JetBrains.Annotations;

namespace ShipEnhancements.Generators.Tests;

[TestSubject(typeof(SESettingsGenerator))]
public class SettingsTests
{
    [Fact]
    public Task GeneratesMockExcelSheetExtensionsCorrectly()
    {
        // The source code to test
        var source =
            """
            using ShipEnhancements.Settings;
            
            namespace GenTests.Tests;
            
            [SESetting("ThrustForce", typeof(float), SESettingType.Number, 3.6)]
            [SESetting("DisableDoor", typeof(bool), SESettingType.Checkbox, false)]
            [SESetting("Grav", typeof(int), SESettingType.Slider, 4)]
            [SESetting("ErnestoNickname", typeof(string), SESettingType.Text, "Ernesto")]
            public static partial class SESettings { }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.VerifySource(source);
    }
}