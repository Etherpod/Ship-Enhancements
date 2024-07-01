using System;

namespace ShipEnhancements;

public static class ModCompatibility
{
    public static bool resourceManagementEnabled;

    public static void Initialize()
    {
        bool Exists(string modID) => ShipEnhancements.Instance.ModHelper.Interaction.ModExists(modID);

        resourceManagementEnabled = Exists("Stonesword.ResourceManagement");
    }
}
