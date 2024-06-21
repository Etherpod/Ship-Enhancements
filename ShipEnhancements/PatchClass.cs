using System;
using HarmonyLib;
using UnityEngine;

namespace ShipEnhancements;

[HarmonyPatch]
public class PatchClass
{
	[HarmonyPrefix]
	[HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.UpdateShipLightInput))]
	public static bool DisableHeadlights(ShipCockpitController __instance)
	{
        if (ShipEnhancements.Instance.HeadlightsDisabled) return false;
        return true;
    }
}
