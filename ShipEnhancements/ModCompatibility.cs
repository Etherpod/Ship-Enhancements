using System;

namespace ShipEnhancements;

public static class ModCompatibility
{
    public static bool VanillaFix { get; private set; }
    public static bool ChristmasStory { get; private set; }
    public static bool Evacuation { get; private set; }
    public static bool EchoHike { get; private set; }

    public static void InitCompatibility()
    {
        var interaction = ShipEnhancements.Instance.ModHelper.Interaction;
        VanillaFix = interaction.ModExists("JohnCorby.VanillaFix");
        ChristmasStory = interaction.ModExists("hearth1an.ChristmasStory");
        Evacuation = interaction.ModExists("2walker2.Evacuation");
        EchoHike = interaction.ModExists("Trifid.TrifidJam3");
    }
}
