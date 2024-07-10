using OWML.Common;
using OWML.ModHelper;
using System;

namespace ShipEnhancements;

public static class ModCompatibility
{
    public static bool resourceManagementEnabled;

    public static void Initialize()
    {
        static bool Exists(string modID) => ShipEnhancements.Instance.ModHelper.Interaction.ModExists(modID);

        resourceManagementEnabled = Exists("Stonesword.ResourceManagement");
    }

    public static bool GetModSetting(string modID, string settingID)
    {
        if (!resourceManagementEnabled) return false;
        IModBehaviour mod = ShipEnhancements.Instance.ModHelper.Interaction.TryGetMod(modID);
        if (mod != null)
        {
            return mod.ModHelper.Config.GetSettingsValue<bool>(settingID);
        }
        return false;
    }
}
