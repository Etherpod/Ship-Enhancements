using UnityEngine;
using ShipEnhancements;
using HarmonyLib;
using System.Reflection;
using NewHorizons.Components.Stars;
using NewHorizons.Components.SizeControllers;
using static ShipEnhancements.ShipEnhancements.Settings;
using NewHorizons.Builder.General;

namespace ShipEnhancementsNH;

public class NHInteraction : MonoBehaviour, INHInteraction
{
    private void Start()
    {
        ShipEnhancements.ShipEnhancements.Instance.AssignNHInterface(this);
        new Harmony("Etherpod.ShipEnhancementsNH").PatchAll(Assembly.GetExecutingAssembly());
    }

    public void AddTempZoneToNHSuns(GameObject tempZonePrefab)
    {
        ShipEnhancements.ShipEnhancements.WriteDebugMessage("adding custom");
        StarController[] nhSuns = FindObjectsOfType<StarController>();
        foreach (StarController nhSun in nhSuns)
        {
            ShipEnhancements.ShipEnhancements.WriteDebugMessage("found custom sun: " + nhSun.gameObject.name);
            if (nhSun.GetComponentInChildren<HeatHazardVolume>() && !nhSun.GetComponentInChildren<TemperatureZone>())
            {
                ShipEnhancements.ShipEnhancements.WriteDebugMessage("sun can support temp zone");
                StarEvolutionController star = nhSun.GetComponentInChildren<StarEvolutionController>();
                TemperatureZone zone = Instantiate(tempZonePrefab, star.transform).GetComponent<TemperatureZone>();
                zone.transform.localPosition = Vector3.zero;
                float sunScale = star.transform.localScale.magnitude / 2;
                zone.SetProperties(100f, sunScale * 2.25f, sunScale, false, 0f, 0f);
            }
        }
    }

    public (Transform, Vector3) GetShipSpawnPoint()
    {
        if (SpawnPointBuilder.ShipSpawn == null) return (null, Vector3.zero);

        return (SpawnPointBuilder.ShipSpawn.transform, SpawnPointBuilder.ShipSpawnOffset);
    }

    public GameObject GetCenterOfUniverse()
    {
        return AstroObjectBuilder.CenterOfUniverse;
    }
}

[HarmonyPatch]
public static class NHInteractionPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StarEvolutionController), "UpdateMainSequence")]
    public static void UpdateSunTempZone(StarEvolutionController __instance, float ____minScale)
    {
        if (!(bool)enableShipTemperature.GetProperty() || ____minScale <= 0) return;

        TemperatureZone tempZone = __instance.GetComponentInChildren<TemperatureZone>();
        if (tempZone != null)
        {
            tempZone.SetScale(__instance.CurrentScale / ____minScale);
        }
    }
}