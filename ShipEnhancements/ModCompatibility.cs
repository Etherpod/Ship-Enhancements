using System;

namespace ShipEnhancements;

public static class ModCompatibility
{
    public static bool VanillaFix { get; private set; }
    public static bool ChristmasStory { get; private set; }
    public static bool Evacuation { get; private set; }
    public static bool EchoHike { get; private set; }
    public static bool AxiomsRefuge { get; private set; }
    public static bool MisfiredJump { get; private set; }
    public static bool TheStrangerTheyAre { get; private set; }
    public static bool Heliostudy { get; private set; }
    public static bool OnARail { get; private set; }
    public static bool UnnamedMystery { get; private set; }
    public static bool NomaisSky { get; private set; }
    public static bool FretsQuest2 { get; private set; }

    public static void InitCompatibility()
    {
        var interaction = ShipEnhancements.Instance.ModHelper.Interaction;
        VanillaFix = interaction.ModExists("JohnCorby.VanillaFix");
        ChristmasStory = interaction.ModExists("hearth1an.ChristmasStory");
        Evacuation = interaction.ModExists("2walker2.Evacuation");
        EchoHike = interaction.ModExists("Trifid.TrifidJam3");
        AxiomsRefuge = interaction.ModExists("MegaPiggy.Axiom");
        MisfiredJump = interaction.ModExists("Echatsum.MisfiredJump");
        TheStrangerTheyAre = interaction.ModExists("AnonymousStrangerOW.TheStrangerTheyAre");
        Heliostudy = interaction.ModExists("2walker2.OWJam5ModProject");
        OnARail = interaction.ModExists("CantAffordaName.OnARail");
        UnnamedMystery = interaction.ModExists("O32.UnnamedMystery");
        NomaisSky = interaction.ModExists("Vambok.NomaiSky");
        FretsQuest2 = interaction.ModExists("Samster68.FretsQuest2");
    }
}
