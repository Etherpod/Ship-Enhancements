using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ShipEnhancements;

[HarmonyPatch]
public class PatchClass
{
    #region DisableHeadlights
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.UpdateShipLightInput))]
    public static bool DisableHeadlights(ShipCockpitController __instance)
    {
        if (ShipEnhancements.Instance.HeadlightsDisabled) return false;
        return true;
    }
    #endregion

    #region DisableOxygen
    [HarmonyPrefix]
    [HarmonyPatch(typeof(OxygenVolume), nameof(OxygenVolume.OnEffectVolumeEnter))]
    public static bool DisableShipOxygen(OxygenVolume __instance)
    {
        if (ShipEnhancements.Instance.oxygenDepleted && __instance.GetComponentInParent<ShipBody>())
        {
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.OnPressInteract))]
    public static bool KeepHelmetOnAtCockpit(ShipCockpitController __instance)
    {
        if (!ShipEnhancements.Instance.oxygenDepleted) return true;

        if (!__instance._playerAtFlightConsole)
        {
            __instance.enabled = true;
            __instance._playerAtFlightConsole = true;
            __instance._enterFlightConsoleTime = Time.time;
            __instance._interactVolume.DisableInteraction();
            Locator.GetToolModeSwapper().UnequipTool();
            if (__instance._controlsLocked && Time.time > __instance._controlsUnlockTime)
            {
                __instance._controlsLocked = false;
            }
            if (!__instance._controlsLocked && !__instance._shipSystemFailure)
            {
                __instance._thrustController.enabled = true;
            }
            __instance._thrustController.SetRollMode(false, 1);
            __instance._playerAttachPoint.transform.localPosition = __instance._raisedAttachPointLocalPos;
            __instance._playerAttachOffset = Vector3.zero;
            __instance._playerAttachPoint.SetAttachOffset(__instance._playerAttachOffset);
            __instance._playerAttachPoint.AttachPlayer();
            __instance._shipAudioController.PlayBuckle();
            for (int i = 0; i < __instance._dimmingLights.Length; i++)
            {
                __instance._dimmingLights[i].SetIntensityScale(__instance._dimmingLightScale);
            }
            /*if (Locator.GetPlayerSuit().IsWearingSuit(true) && !__instance._shipSystemFailure)
            {
                Locator.GetPlayerSuit().RemoveHelmet();
            }*/
            __instance._cockpitCollider.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            for (int j = 0; j < __instance._fogLightOcclusionColliders.Length; j++)
            {
                __instance._fogLightOcclusionColliders[j].enabled = true;
            }
            for (int k = 0; k < __instance._shipCanvases.Length; k++)
            {
                __instance._shipCanvases[k].SetGameplayActive(false);
            }
            if (__instance._controlsLocked || __instance._shipSystemFailure)
            {
                RumbleManager.SetShipThrottleLocked();
            }
            else if (__instance._thrustController.RequiresIgnition() && __instance._landingManager.IsLanded())
            {
                RumbleManager.SetShipThrottleCold();
            }
            else
            {
                RumbleManager.SetShipThrottleNormal();
            }
            OWInput.ChangeInputMode(InputMode.ShipCockpit);
            GlobalMessenger<OWRigidbody>.FireEvent("EnterFlightConsole", __instance._shipBody);
        }
        return false;
    }
    #endregion

    #region DisableGravity
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCameraController), nameof(PlayerCameraController.UpdateInput))]
    public static bool AllowZeroGCockpitFreeLook(PlayerCameraController __instance, float deltaTime)
    {
        bool flag = __instance._shipController != null && __instance._shipController.AllowFreeLook() && OWInput.IsPressed(InputLibrary.freeLook, 0f);
        bool flag2 = OWInput.IsInputMode(InputMode.Character | InputMode.ScopeZoom | InputMode.NomaiRemoteCam | InputMode.PatchingSuit);
        if (__instance._isSnapping || __instance._isLockedOn
            || (PlayerState.InZeroG() && PlayerState.IsWearingSuit() && !PlayerState.AtFlightConsole()) || (!flag2 && !flag))
        {
            return false;
        }
        bool flag3 = Locator.GetAlarmSequenceController() != null && Locator.GetAlarmSequenceController().IsAlarmWakingPlayer();
        Vector2 vector = Vector2.one;
        vector *= ((__instance._zoomed || flag3) ? PlayerCameraController.ZOOM_SCALAR : 1f);
        vector *= __instance._playerCamera.fieldOfView / __instance._initFOV;
        if (Time.timeScale > 1f)
        {
            vector /= Time.timeScale;
        }
        float num = deltaTime;
        if (InputLibrary.look.AxisID == AxisIdentifier.KEYBD_MOUSE
            || InputLibrary.look.AxisID == AxisIdentifier.KEYBD_MOUSEX
            || InputLibrary.look.AxisID == AxisIdentifier.KEYBD_MOUSEY)
        {
            num = 0.01666667f;
        }
        if (flag)
        {
            Vector2 axisValue = OWInput.GetAxisValue(InputLibrary.look, InputMode.All);
            __instance._degreesX += axisValue.x * 180f * vector.x * num;
            __instance._degreesY += axisValue.y * 180f * vector.y * num;
            return false;
        }
        float num2 = (OWInput.UsingGamepad() ? PlayerCameraController.GAMEPAD_LOOK_RATE_Y : PlayerCameraController.LOOK_RATE);
        __instance._degreesY += OWInput.GetAxisValue(InputLibrary.look, InputMode.All).y * num2 * vector.y * num;
        return false;
    }
    #endregion

    #region ResourceDrainMultiplier
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipResources), nameof(ShipResources.DrainOxygen))]
    public static bool ApplyOxygenDrainMultiplier(ShipResources __instance, float amount)
    {
        __instance._currentOxygen = Mathf.Max(__instance._currentOxygen - (amount * ShipEnhancements.Instance.OxygenDrainMultiplier), 0f);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipResources), nameof(ShipResources.DrainFuel))]
    public static bool ApplyFuelDrainMultiplier(ShipResources __instance, float amount)
    {
        __instance._currentFuel = Mathf.Max(__instance._currentFuel - (amount * ShipEnhancements.Instance.FuelDrainMultiplier), 0f);
        return false;
    }
    #endregion

    #region DamageMultiplier
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipHull), nameof(ShipHull.FixedUpdate))]
    public static bool ApplyHullDamageMultiplier(ShipHull __instance)
    {
        if (__instance._debugImpact)
        {
            __instance._integrity = 0.5f;
            __instance._damaged = true;
            __instance._debugImpact = false;

            var eventDelegate1 = (MulticastDelegate)typeof(ShipHull).GetField("OnDamaged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__instance);
            if (eventDelegate1 != null)
            {
                foreach (var handler in eventDelegate1.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, [__instance]);
                }
            }

            if (__instance._damageEffect != null)
            {
                __instance._damageEffect.SetEffectBlend(1f - __instance._integrity);
            }
            __instance.enabled = false;
        }
        if (__instance._dominantImpact != null)
        {
            float num = Mathf.InverseLerp(30f * ShipEnhancements.Instance.DamageSpeedMultiplier,
                200f * ShipEnhancements.Instance.DamageSpeedMultiplier, __instance._dominantImpact.speed);
            if (num > 0f)
            {
                float num2 = 0.15f;
                if (num < num2 && __instance._integrity > 1f - num2)
                {
                    num = num2;
                }
                num *= ShipEnhancements.Instance.DamageMultiplier;
                __instance._integrity = Mathf.Max(__instance._integrity - num, 0f);
                if (!__instance._damaged)
                {
                    __instance._damaged = true;

                    var eventDelegate2 = (MulticastDelegate)typeof(ShipHull).GetField("OnDamaged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__instance);
                    if (eventDelegate2 != null)
                    {
                        foreach (var handler in eventDelegate2.GetInvocationList())
                        {
                            handler.Method.Invoke(handler.Target, [__instance]);
                        }
                    }
                }
                if (__instance._damageEffect != null)
                {
                    __instance._damageEffect.SetEffectBlend(1f - __instance._integrity);
                }
            }
            int num3 = 0;
            while (num3 < __instance._components.Length && (__instance._components[num3] == null || __instance._components[num3].isDamaged || !__instance._components[num3].ApplyImpact(__instance._dominantImpact)))
            {
                num3++;
            }

            var eventDelegate3 = (MulticastDelegate)typeof(ShipHull).GetField("OnImpact", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__instance);
            if (eventDelegate3 != null)
            {
                foreach (var handler in eventDelegate3.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, [__instance._dominantImpact, num]);
                }
            }

            __instance._dominantImpact = null;
        }
        __instance.enabled = false;

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipComponent), nameof(ShipComponent.ApplyImpact))]
    public static bool ApplyComponentDamageMultiplier(ShipComponent __instance, ImpactData impact)
    {
        if (__instance._damaged)
        {
            return false;
        }
        if (UnityEngine.Random.value / ShipEnhancements.Instance.DamageMultiplier
            < __instance._damageProbabilityCurve.Evaluate(impact.speed / ShipEnhancements.Instance.DamageSpeedMultiplier))
        {
            __instance.SetDamaged(true);
            return true;
        }
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.ShouldExitShipToRepair))]
    public static void FixExitShipToRepairNotification(ShipDamageController __instance, ref bool __result)
    {
        bool flag = false;
        for (int j = 0; j < __instance._shipComponents.Length; j++)
        {
            if (__instance._shipComponents[j].isDamaged && __instance._shipComponents[j].componentName != UITextType.ShipPartGravity
                && __instance._shipComponents[j].componentName != UITextType.ShipPartReactor
                && !(ShipEnhancements.Instance.LandingCameraDisabled && __instance._shipComponents[j].componentName == UITextType.ShipPartCamera)
                && !(ShipEnhancements.Instance.HeadlightsDisabled && __instance._shipComponents[j].componentName == UITextType.ShipPartLights))
            {
                flag = true;
            }
        }
        __result = flag;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.OnImpact))]
    public static bool ApplyExplosionDamageMultiplier(ShipDamageController __instance, ImpactData impact)
    {
        if (impact.otherCollider.attachedRigidbody != null && impact.otherCollider.attachedRigidbody.CompareTag("Player") && PlayerState.IsInsideShip())
        {
            return false;
        }
        if (impact.speed >= 300f * ShipEnhancements.Instance.DamageSpeedMultiplier && !__instance._exploded)
        {
            __instance.Explode(false);
            return false;
        }
        if (!__instance._invincible)
        {
            for (int i = 0; i < __instance._shipModules.Length; i++)
            {
                __instance._shipModules[i].ApplyImpact(impact);
            }
        }
        return false;
    }
    #endregion

    #region OxygenRefill
    [HarmonyPostfix]
    [HarmonyPatch(typeof(OxygenVolume), nameof(OxygenVolume.PlaysRefillAudio))]
    public static void PlayRefillAudio(OxygenVolume __instance, ref bool __result)
    {
        if (__instance.GetComponentInParent<ShipBody>() && ShipEnhancements.Instance.refillingOxygen)
        {
            __result = true;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipResources), nameof(ShipResources.Update))]
    public static bool RefillShipOxygen(ShipResources __instance)
    {
        if (ShipEnhancements.Instance.OxygenDisabled || !ShipEnhancements.Instance.ShipOxygenRefill) return true;

        if (__instance._killingResources)
        {
            __instance.DebugKillResources();
            return false;
        }
        float magnitude = __instance._shipThruster.GetLocalAcceleration().magnitude;
        if (magnitude > 0f)
        {
            __instance.DrainFuel(magnitude * 0.1f * Time.deltaTime);
        }
        if (__instance._currentFuel <= 0f && !NotificationManager.SharedInstance.IsPinnedNotification(__instance._fuelDepletedNotification))
        {
            NotificationManager.SharedInstance.PostNotification(__instance._fuelDepletedNotification, true);
        }
        else if (__instance._currentFuel > 0f && NotificationManager.SharedInstance.IsPinnedNotification(__instance._fuelDepletedNotification))
        {
            NotificationManager.SharedInstance.UnpinNotification(__instance._fuelDepletedNotification);
        }
        if (__instance._hullBreach)
        {
            __instance.DrainOxygen(1000f * Time.deltaTime);
        }
        else
        {
            if (ShipEnhancements.Instance.IsShipInOxygen())
            {
                __instance.AddOxygen(100f * Time.deltaTime);
            }
            else if (PlayerState.IsInsideShip())
            {
                __instance.DrainOxygen(0.13f * Time.deltaTime);
            }
        }
        return false;
    }
    #endregion

    #region DisableShipRepair
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipComponent), nameof(ShipComponent.Awake))]
    public static void DisableShipComponentRepair(ShipComponent __instance)
    {
        if (!ShipEnhancements.Instance.ShipRepairDisabled) return;

        __instance._repairReceiver._repairDistance = 0f;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipHull), nameof(ShipHull.Start))]
    public static void DisableShipHullRepair(ShipHull __instance)
    {
        if (!ShipEnhancements.Instance.ShipRepairDisabled) return;

        for (int i = 0; i < __instance._colliders.Length; i++)
        {
            if (__instance._colliders[i].TryGetComponent(out RepairReceiver repairReceiver))
            {
                repairReceiver._repairDistance = 0f;
            }
        }
    }
    #endregion

    #region GravityLandingGear
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LandingPadSensor), nameof(LandingPadSensor.Awake))]
    public static void AddGravityComponent(LandingPadSensor __instance)
    {
        __instance.gameObject.AddComponent<GravityLandingGear>();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LandingPadSensor), nameof(LandingPadSensor.OnTriggerEnter))]
    public static void EnableGravity(LandingPadSensor __instance)
    {
        //__instance.transform.GetChild(0).gameObject.SetActive(true);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LandingPadSensor), nameof(LandingPadSensor.OnTriggerExit))]
    public static void DisableGravity(LandingPadSensor __instance)
    {
        //__instance.transform.GetChild(0).gameObject.SetActive(false);
    }
    #endregion
}
