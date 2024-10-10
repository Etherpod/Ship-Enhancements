using UnityEngine;
using QSB.ShipSync;
using QSB.ShipSync.TransformSync;
using QSB.Player;
using HarmonyLib;
using QSB.RespawnSync;
using ShipEnhancements;
using System.Reflection;
using QSB.WorldSync;
using QSB.TimeSync;

namespace ShipEnhancementsQSB;

public class QSBInteraction : MonoBehaviour, IQSBInteraction
{
    private void Start()
    {
        ShipEnhancements.ShipEnhancements.Instance.AssignQSBInterface(this);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem)
            {
                return;
            }

            ShipEnhancements.ShipEnhancements.WriteDebugMessage("ah");

            ShipEnhancements.ShipEnhancements.Instance.ModHelper.Events.Unity.FireInNUpdates(() =>
            {
                if ((bool)ShipEnhancements.ShipEnhancements.Settings.addPortableCampfire.GetProperty())
                {
                    QSBWorldSync.Init<QSBPortableCampfireItem, PortableCampfireItem>();
                }
            }, 2);
        };
    }

    public bool FlightConsoleOccupied()
    {
        return ShipManager.Instance.CurrentFlyer != uint.MaxValue;
    }

    public Vector3 GetShipAcceleration()
    {
        return ShipTransformSync.LocalInstance?.ThrusterVariableSyncer?.AccelerationSyncer?.Value
            ?? Vector3.zero;
    }

    public int GetPlayersInShip()
    {
        int num = 0;

        foreach (uint id in ShipEnhancements.ShipEnhancements.QSBAPI.GetPlayerIDs())
        {
            if (QSBPlayerManager.GetPlayer(id).IsInShip)
            {
                num++;
            }
        }

        return num;
    }

    public GameObject GetShipRecoveryPoint()
    {
        return SELocator.GetShipTransform().GetComponentInChildren<ShipRecoveryPoint>().gameObject;
    }

    public bool IsRecoveringAtShip()
    {
        return QSBInteractionPatches.RecoveringAtShip;
    }

    public bool IsTimeFlowing()
    {
        return WakeUpSync.LocalInstance.HasWokenUp;
    }
}

[HarmonyPatch]
public static class QSBInteractionPatches
{
    public static bool RecoveringAtShip;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipRecoveryPoint), "OnGainFocus")]
    public static void DisableRefuelPrompt(ShipRecoveryPoint __instance)
    {
        var playerResources = SELocator.GetPlayerResources();

        var needsHealing = playerResources.GetHealthFraction() != 1f;
        var needsRefueling = playerResources.GetFuelFraction() != 1f;
        var canRefuel = SELocator.GetShipResources().GetFuel() > 0f;
        UITextType uiTextType;
        bool keyCommandVisible;

        ShipEnhancements.ShipEnhancements.WriteDebugMessage("Can refuel: " + canRefuel);
        ShipEnhancements.ShipEnhancements.WriteDebugMessage("Needs healing: " + needsHealing);

        if (needsHealing && needsRefueling && canRefuel)
        {
            uiTextType = UITextType.RefillPrompt_0;
            keyCommandVisible = true;
        }
        else if (needsHealing)
        {
            uiTextType = UITextType.RefillPrompt_2;
            keyCommandVisible = true;
        }
        else if (needsRefueling && canRefuel)
        {
            uiTextType = UITextType.RefillPrompt_4;
            keyCommandVisible = true;
        }
        else if (!canRefuel)
        {
            uiTextType = UITextType.RefillPrompt_9;
            keyCommandVisible = false;
        }
        else
        {
            uiTextType = UITextType.RefillPrompt_7;
            keyCommandVisible = false;
        }

        MultipleInteractionVolume interactVolume = (MultipleInteractionVolume)typeof(ShipRecoveryPoint).GetField("_interactVolume", BindingFlags.NonPublic
            | BindingFlags.Public | BindingFlags.Instance).GetValue(__instance);
        int refillIndex = (int)typeof(ShipRecoveryPoint).GetField("_refillIndex", BindingFlags.NonPublic | BindingFlags.Public
                | BindingFlags.Instance).GetValue(__instance);

        interactVolume.ChangePrompt(uiTextType, refillIndex);

        if (PlayerState.IsWearingSuit())
        {
            interactVolume.EnableSingleInteraction(true, refillIndex);
            interactVolume.SetKeyCommandVisible(keyCommandVisible, refillIndex);
            interactVolume.GetInteractionAt(refillIndex).cachedScreenPrompt.SetDisplayState(ScreenPrompt.DisplayState.Normal);
        }
        else
        {
            interactVolume.EnableSingleInteraction(false, refillIndex);
            interactVolume.SetKeyCommandVisible(false, refillIndex);
            interactVolume.GetInteractionAt(refillIndex).cachedScreenPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipRecoveryPoint), "HandleRecovery")]
    public static bool DisableFuelRefill(ShipRecoveryPoint __instance)
    {
        PlayerResources playerResources = SELocator.GetPlayerResources();

        var canRefuel = playerResources.GetFuelFraction() != 1f && SELocator.GetShipResources().GetFuel() > 0f;
        var needsHealing = playerResources.GetHealthFraction() != 1f;
        var needsRefill = false;

        if (canRefuel)
        {
            needsRefill = true;
        }
        if (needsHealing)
        {
            needsRefill = true;
        }

        if (needsRefill)
        {
            playerResources.StartRefillResources(canRefuel, needsHealing);

            PlayerAudioController audioController = Locator.GetPlayerAudioController();
            if (audioController != null)
            {
                if (canRefuel)
                {
                    audioController.PlayRefuel();
                }
                if (needsHealing)
                {
                    audioController.PlayMedkit();
                }
            }

            typeof(ShipRecoveryPoint).GetField("_recovering", BindingFlags.NonPublic | BindingFlags.Public
                | BindingFlags.Instance).SetValue(__instance, true);

            RecoveringAtShip = true;

            __instance.enabled = true;
            return false;
        }

        MultipleInteractionVolume volume = (MultipleInteractionVolume)typeof(ShipRecoveryPoint).GetField("_interactVolume", BindingFlags.NonPublic | BindingFlags.Public
                | BindingFlags.Instance).GetValue(__instance);
        volume.ResetInteraction();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipRecoveryPoint), "Update")]
    public static bool UpdateRecoveringState(ShipRecoveryPoint __instance)
    {
        FieldInfo recoveringField = typeof(ShipRecoveryPoint).GetField("_recovering", BindingFlags.NonPublic | BindingFlags.Public
                | BindingFlags.Instance);

        bool recovering = (bool)recoveringField.GetValue(__instance);
        var playerResources = SELocator.GetPlayerResources();

        if (recovering)
        {
            var doneRecovering = true;
            if (playerResources.GetFuelFraction() < 1f && SELocator.GetShipResources().GetFuel() > 0f)
            {
                doneRecovering = false;
            }

            if (playerResources.GetHealthFraction() < 1f)
            {
                doneRecovering = false;
            }

            if (doneRecovering)
            {
                playerResources.StopRefillResources();
                recovering = false;

                recoveringField.SetValue(__instance, recovering);
                RecoveringAtShip = recovering;
            }
        }

        if (!recovering)
        {
            __instance.enabled = false;
        }

        return false;
    }
}