using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using ShipEnhancements;
using GeneralEnhancements;

namespace ShipEnhancementsGE;

public class GEInteraction : ModBehaviour, IGEInteraction
{
    public static ModMain modMain;

    public void Start()
    {
        ShipEnhancements.ShipEnhancements.Instance.AssignGEInterface(this);
        new Harmony("Etherpod.ShipEnhancementsGE").PatchAll(Assembly.GetExecutingAssembly());

        modMain = FindObjectOfType<ModMain>();
    }

    public bool IsContinuousMatchVelocityEnabled()
    {
        return ContinuousMatchVelocity.matchVelocityShip;
    }

    public void StopContinuousMatchVelocity()
    {
        ContinuousMatchVelocity.StopShipMatch();
    }

    public void EnableContinuousMatchVelocity()
    {
        ShipEnhancements.ShipEnhancements.WriteDebugMessage("gabagool");
        if (modMain == null) return;
        var features = typeof(ModMain).GetField("features", BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance).GetValue(modMain) as Feature[];
        foreach (var feature in features)
        {
            if (feature is ContinuousMatchVelocity)
            {
                typeof(ContinuousMatchVelocity).GetProperty("matchVelocityShip", BindingFlags.Static | BindingFlags.Public
                    | BindingFlags.NonPublic).SetValue(feature, true);
                return;
            }
        }
    }
}

[HarmonyPatch]
public static class GEPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ContinuousMatchVelocity), nameof(ContinuousMatchVelocity.StopShipMatch))]
    public static void UpdateMatchVelocityButton()
    {
        if (SELocator.GetAutopilotPanelController().IsMatchVelocitySelected())
        {
            SELocator.GetAutopilotPanelController().CancelMatchVelocity();
        }
    }
}