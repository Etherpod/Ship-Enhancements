using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using OWML.Common;
using ShipEnhancements.ModMenu;
using OWML.ModHelper.Menus.NewMenuSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

[HarmonyPatch]
public static class PatchClass
{
    #region DisableHeadlights
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.UpdateShipLightInput))]
    public static bool DisableHeadlights(ShipCockpitController __instance)
    {
        if ((string)shipHornType.GetProperty() != "None"
            && OWInput.IsNewlyPressed(InputLibrary.flashlight, InputMode.ShipCockpit)
            && !__instance._shipSystemFailure
            && SELocator.GetShipTransform().GetComponentInChildren<Signalscope>().IsEquipped()
            && (SELocator.GetSignalscopeComponent() == null || !SELocator.GetSignalscopeComponent().isDamaged))
        {
            SELocator.GetShipTransform().GetComponentInChildren<ShipHornController>()?.PlayHorn();
            if (ShipEnhancements.InMultiplayer)
            {
                foreach (uint id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendHonkHorn(id);
                }
            }
            return false;
        }

        if (ShipEnhancements.Instance.disableShipHeadlights) return false;

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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(OxygenDetector), nameof(OxygenDetector.GetDetectOxygen))]
    public static void DisableOxygenDetection(OxygenDetector __instance, ref bool __result)
    {
        if ((bool)disableShipOxygen.GetProperty() && __instance.gameObject.CompareTag("ShipDetector"))
        {
            __result = false;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.OnPressInteract))]
    public static bool KeepHelmetOnAtCockpit(ShipCockpitController __instance, bool __runOriginal)
    {
        if (!__runOriginal) return false;

        bool run = (bool)keepHelmetOn.GetProperty() && (ShipEnhancements.Instance.oxygenDepleted
            || (bool)disableFluidPrevention.GetProperty());
        if (!run) return true;

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.StartSleeping))]
    public static bool KeepHelmetOnWhenSleeping(Campfire __instance)
    {
        bool portableCampfire = (bool)addPortableCampfire.GetProperty() && __instance is PortableCampfire;
        bool shouldKeepHelmetOn = (bool)keepHelmetOn.GetProperty() && !SELocator.GetPlayerResources().IsOxygenPresent()
            && PlayerState.IsWearingSuit();

        if (!portableCampfire && !shouldKeepHelmetOn) return true;
        else if (portableCampfire && !Locator.GetPlayerController().IsGrounded())
        {
            __instance._interactVolume.ResetInteraction();
            return false;
        }

        if (__instance.CheckUnequipToolWhileSleeping())
        {
            Locator.GetToolModeSwapper().UnequipTool();
        }
        __instance._attachPoint.AttachPlayer();
        __instance._interactVolume.DisableInteraction();
        Vector3 localPosition = Locator.GetPlayerTransform().localPosition;
        Vector3 vector = new Vector3(localPosition.x, 0f, localPosition.z);
        Vector3 vector2 = 2f * vector.normalized + Vector3.up;
        if (portableCampfire)
        {
            vector2 = localPosition;
        }
        __instance._attachPoint.SetAttachOffset(vector2);
        if (__instance._lookUpWhileSleeping)
        {
            __instance._lockOnTargeting.LockOn(__instance.transform, Vector3.up * 10f, 1f, true, 1f);
        }
        else
        {
            __instance._lockOnTargeting.LockOn(__instance.transform, Vector3.up * 0.75f, 1f, true, 1f);
        }
        Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>().CloseEyes(3f);
        Locator.GetAudioMixer().MixSleepAtCampfire(3f);
        Locator.GetPlayerAudioController().OnStartSleepingAtCampfire(__instance is DreamCampfire);
        __instance._fastForwardStartTime = Time.timeSinceLevelLoad + 3f;
        __instance._isPlayerSleeping = true;
        Locator.GetPromptManager().AddScreenPrompt(__instance._wakePrompt, PromptPosition.Center, false);
        __instance._sleepPrompt.SetVisibility(false);
        __instance._wakePrompt.SetVisibility(false);
        OWInput.ChangeInputMode(InputMode.None);
        if (!shouldKeepHelmetOn && Locator.GetPlayerSuit().IsWearingSuit(true))
        {
            Locator.GetPlayerSuit().RemoveHelmet();
        }
        Locator.GetFlashlight().TurnOff(false);
        GlobalMessenger<bool>.FireEvent("StartSleepingAtCampfire", __instance is DreamCampfire);

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.StopSleeping))]
    public static bool KeepHelmetOnWhenStopSleeping(Campfire __instance, bool sudden)
    {
        if (!(bool)keepHelmetOn.GetProperty() || (!Locator.GetPlayerSuit().IsWearingHelmet()
            && (SELocator.GetPlayerResources().IsOxygenPresent() || !PlayerState.IsWearingSuit()))) return true;

        if (!__instance._isPlayerSleeping)
        {
            return false;
        }
        if (__instance._isTimeFastForwarding)
        {
            __instance.StopFastForwarding();
        }
        __instance._attachPoint.DetachPlayer();
        __instance._lockOnTargeting.BreakLock();
        __instance._interactVolume.EnableInteraction();
        if (__instance._lookUpWhileSleeping || PlayerState.InZeroG())
        {
            Locator.GetPlayerCamera().GetComponent<PlayerCameraController>().CenterCamera(50f, true);
        }
        Locator.GetPlayerCamera().GetComponent<PlayerCameraEffectController>().OpenEyes(1f, sudden);
        Locator.GetAudioMixer().UnmixSleepAtCampfire(sudden ? 1f : 3f);
        Locator.GetPlayerAudioController().OnStopSleepingAtCampfire(sudden || Time.timeSinceLevelLoad - __instance._fastForwardStartTime > 60f, sudden);
        __instance._isPlayerSleeping = false;
        Locator.GetPromptManager().RemoveScreenPrompt(__instance._wakePrompt);
        OWInput.ChangeInputMode(InputMode.Character);
        /*if (Locator.GetPlayerSuit().IsWearingSuit(true))
        {
            Locator.GetPlayerSuit().PutOnHelmetAfterDelay(2f);
        }*/
        __instance.OnStopSleeping();
        GlobalMessenger.FireEvent("StopSleepingAtCampfire");

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.StartRoasting))]
    public static bool KeepHelmetOnWhenRoasting(Campfire __instance)
    {
        bool portableCampfire = (bool)addPortableCampfire.GetProperty() && __instance is PortableCampfire;
        bool shouldKeepHelmetOn = (bool)keepHelmetOn.GetProperty() && !SELocator.GetPlayerResources().IsOxygenPresent()
            && PlayerState.IsWearingSuit();

        if (!portableCampfire && !shouldKeepHelmetOn) return true;
        else if (portableCampfire && !Locator.GetPlayerController().IsGrounded())
        {
            __instance._interactVolume.ResetInteraction();
            return false;
        }

        Locator.GetToolModeSwapper().UnequipTool();
        __instance._attachPoint.AttachPlayer();

        Vector3 localPosition = Locator.GetPlayerTransform().localPosition;
        Vector3 vector = new Vector3(localPosition.x, 0f, localPosition.z);
        Vector3 vector2 = 2f * vector.normalized + Vector3.up;
        if (portableCampfire)
        {
            Vector3 projectedVector = Vector3.Project(localPosition, vector2);
            float ratio = projectedVector.sqrMagnitude / vector2.sqrMagnitude;
            RoastingStickController stickController = Locator.GetPlayerTransform().GetComponentInChildren<RoastingStickController>();
            stickController._stickMaxZ = Mathf.LerpUnclamped(stickController._stickMinZ, baseRoastingStickMaxZ, ratio);
            vector2 = localPosition;
        }

        __instance._attachPoint.SetAttachOffset(vector2);

        Vector3 vector3 = Vector3.up * 0.75f;
        __instance._lockOnTargeting.LockOn(__instance.transform, vector3, 1f, true, 1f);
        __instance._isPlayerRoasting = true;
        GlobalMessenger<Campfire>.FireEvent("EnterRoastingMode", __instance);
        if (!shouldKeepHelmetOn && Locator.GetPlayerSuit().IsWearingSuit(true))
        {
            Locator.GetPlayerSuit().RemoveHelmet();
        }
        if (__instance._canSleepHere)
        {
            __instance._sleepPrompt.SetVisibility(false);
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.StopRoasting))]
    public static bool KeepHelmetOnWhenStopRoasting(Campfire __instance)
    {
        Locator.GetPlayerTransform().GetComponentInChildren<RoastingStickController>()._stickMaxZ = baseRoastingStickMaxZ;

        if (!(bool)keepHelmetOn.GetProperty() || (!Locator.GetPlayerSuit().IsWearingHelmet()
            && (SELocator.GetPlayerResources().IsOxygenPresent() || !PlayerState.IsWearingSuit()))) return true;

        if (!__instance._isPlayerRoasting)
        {
            return false;
        }
        __instance._attachPoint.DetachPlayer();
        __instance._lockOnTargeting.BreakLock();
        __instance._interactVolume.ResetInteraction();
        if (PlayerState.InZeroG())
        {
            Locator.GetPlayerCamera().GetComponent<PlayerCameraController>().CenterCamera(50f, true);
        }
        __instance._isPlayerRoasting = false;
        GlobalMessenger.FireEvent("ExitRoastingMode");
        /*if (Locator.GetPlayerSuit().IsWearingSuit(true))
        {
            Locator.GetPlayerSuit().PutOnHelmet();
        }*/

        return false;
    }
    #endregion

    #region DisableGravity
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCameraController), nameof(PlayerCameraController.UpdateInput))]
    public static bool AllowZeroGCockpitFreeLook(PlayerCameraController __instance, float deltaTime)
    {
        //if (!(bool)zeroGravityCockpitFreeLook.GetProperty()) return true;

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
        if ((float)oxygenDrainMultiplier.GetProperty() == 1f) return true;

        __instance._currentOxygen = Mathf.Clamp(__instance._currentOxygen - (amount * (float)oxygenDrainMultiplier.GetProperty()), 0f, __instance._maxOxygen);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipResources), nameof(ShipResources.DrainFuel))]
    public static bool ApplyFuelDrainMultiplier(ShipResources __instance, float amount)
    {
        if ((float)fuelDrainMultiplier.GetProperty() == 1f) return true;

        __instance._currentFuel = Mathf.Clamp(__instance._currentFuel - (amount * (float)fuelDrainMultiplier.GetProperty()), 0f, __instance._maxFuel);
        return false;
    }
    #endregion

    #region DamageMultiplier
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipHull), nameof(ShipHull.FixedUpdate))]
    public static bool ApplyHullDamageMultiplier(ShipHull __instance)
    {
        if (((float)shipDamageMultiplier.GetProperty() == 1 && (float)shipDamageSpeedMultiplier.GetProperty() == 1
            && SELocator.GetShipTemperatureDamageController() == null)
            || ShipEnhancements.InMultiplayer)
        {
            return true;
        }

        if ((float)shipDamageMultiplier.GetProperty() <= 0f)
        {
            return false;
        }

        float damageMultiplier = Mathf.Max((float)shipDamageMultiplier.GetProperty(), 0f);
        float damageSpeedMultiplier = Mathf.Max((float)shipDamageSpeedMultiplier.GetProperty(), 0f);

        var tempDamage = SELocator.GetShipTemperatureDamageController();
        if (tempDamage != null)
        {
            var tempMultiplier = tempDamage.GetDamageMultipliers();
            damageMultiplier *= tempMultiplier.damageMultiplier;
            damageSpeedMultiplier *= tempMultiplier.speedMultiplier;
        }

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
            float num = Mathf.InverseLerp(30f * damageSpeedMultiplier,
                200f * damageSpeedMultiplier, __instance._dominantImpact.speed);
            if (num > 0f)
            {
                float num2 = 0.15f;
                if (num < num2 && __instance._integrity > 1f - num2)
                {
                    num = num2;
                }
                num *= damageMultiplier;
                __instance._integrity = Mathf.Max(__instance._integrity - num, 0f);
                if (!__instance._damaged)
                {
                    __instance._damaged = true;

                    var eventDelegate2 = (MulticastDelegate)typeof(ShipHull).GetField("OnDamaged", BindingFlags.Instance
                        | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__instance);
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
            while (num3 < __instance._components.Length && (__instance._components[num3] == null
                || __instance._components[num3].isDamaged || !__instance._components[num3].ApplyImpact(__instance._dominantImpact)))
            {
                num3++;
            }

            var eventDelegate3 = (MulticastDelegate)typeof(ShipHull).GetField("OnImpact", BindingFlags.Instance
                | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__instance);
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
        if ((float)shipDamageMultiplier.GetProperty() == 1 && (float)shipDamageSpeedMultiplier.GetProperty() == 1
            && SELocator.GetShipTemperatureDamageController() == null)
        {
            return true;
        }

        if (__instance._damaged
            || (float)shipDamageMultiplier.GetProperty() <= 0f)
        {
            return false;
        }

        float damageMultiplier = Mathf.Max((float)shipDamageMultiplier.GetProperty(), 0f);
        float damageSpeedMultiplier = Mathf.Max((float)shipDamageSpeedMultiplier.GetProperty(), 0f);

        var tempDamage = SELocator.GetShipTemperatureDamageController();
        if (tempDamage != null)
        {
            var tempMultiplier = tempDamage.GetDamageMultipliers();
            damageMultiplier *= tempMultiplier.damageMultiplier;
            damageSpeedMultiplier *= tempMultiplier.speedMultiplier;
        }

        if (UnityEngine.Random.value / damageMultiplier
            < __instance._damageProbabilityCurve.Evaluate(impact.speed / damageSpeedMultiplier))
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
        if (!__result) return;

        if (ShipRepairLimitController.CanRepair())
        {
            for (int i = 0; i < __instance._shipComponents.Length; i++)
            {
                ShipComponent comp = __instance._shipComponents[i];
                if (comp.isDamaged && comp._repairReceiver._repairDistance > 0
                    && comp.componentName != UITextType.ShipPartReactor
                    && comp.componentName != UITextType.ShipPartGravity)
                {
                    return;
                }
            }
        }

        __result = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.OnImpact))]
    public static bool ApplyExplosionDamageMultiplier(ShipDamageController __instance, ImpactData impact)
    {
        if ((float)shipDamageMultiplier.GetProperty() <= 0f)
        {
            return false;
        }

        float damageMultiplier = Mathf.Max((float)shipDamageMultiplier.GetProperty(), 0f);
        float damageSpeedMultiplier = Mathf.Max((float)shipDamageSpeedMultiplier.GetProperty(), 0f);

        float explosionMultiplier = damageSpeedMultiplier
                / (damageMultiplier != 1f ? Mathf.Lerp(damageMultiplier, 1f, 0.5f) : 1f);

        if (impact.otherCollider.attachedRigidbody != null && impact.otherCollider.attachedRigidbody.CompareTag("Player") && PlayerState.IsInsideShip())
        {
            return false;
        }
        if (impact.speed >= 300f * explosionMultiplier && !__instance._exploded)
        {
            ErnestoDetectiveController.ItWasExplosion(fromSpeed: true);
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HighSpeedImpactSensor), nameof(HighSpeedImpactSensor.FixedUpdate))]
    public static void DisableDamageExplode(HighSpeedImpactSensor __instance)
    {
        if (__instance._dieNextUpdate && __instance.gameObject.CompareTag("Ship"))
        {
            if ((float)shipDamageMultiplier.GetProperty() <= 0f)
            {
                __instance._dieNextUpdate = false;
            }
            else
            {
                ErnestoDetectiveController.ItWasExplosion(fromSpeed: true);
            }
        }
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
        if (((bool)disableShipOxygen.GetProperty() || !(bool)shipOxygenRefill.GetProperty()) && !ShipEnhancements.InMultiplayer) return true;

        if (__instance._killingResources)
        {
            __instance.DebugKillResources();
            return false;
        }
        if (ShipEnhancements.InMultiplayer)
        {
            float magnitude = ShipEnhancements.QSBInteraction.GetShipAcceleration().magnitude;
            if (magnitude > 0f)
            {
                __instance.DrainFuel(magnitude * 0.1f * Time.deltaTime);
            }
        }
        else
        {
            float magnitude = __instance._shipThruster.GetLocalAcceleration().magnitude;
            if (magnitude > 0f)
            {
                __instance.DrainFuel(magnitude * 0.1f * Time.deltaTime);
            }
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
            if ((bool)shipOxygenRefill.GetProperty() && ShipEnhancements.Instance.IsShipInOxygen())
            {
                __instance.AddOxygen(100f * Time.deltaTime * (float)oxygenRefillMultiplier.GetProperty());
            }
            else
            {
                if (ShipEnhancements.InMultiplayer)
                {
                    __instance.DrainOxygen(0.13f * Time.deltaTime * ShipEnhancements.QSBInteraction.GetPlayersInShip());
                }
                else if (PlayerState.IsInsideShip())
                {
                    __instance.DrainOxygen(0.13f * Time.deltaTime);
                }
            }
        }
        return false;
    }
    #endregion

    #region GravityLandingGear
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LandingPadSensor), nameof(LandingPadSensor.Awake))]
    public static void AddGravityComponent(LandingPadSensor __instance)
    {
        if ((bool)enableGravityLandingGear.GetProperty())
        {
            GameObject gravityPadObj = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/GravityLandingPad.prefab");
            ShipEnhancements.CreateObject(gravityPadObj, __instance.transform);
        }
    }
    #endregion

    #region DisableAutoRoll
    [HarmonyPrefix]
    [HarmonyPatch(typeof(FluidVolume), nameof(FluidVolume.Start))]
    public static void DisableAutoRoll(FluidVolume __instance)
    {
        if ((bool)disableAirAutoRoll.GetProperty() && __instance._fluidType == FluidVolume.Type.AIR)
        {
            __instance._allowShipAutoroll = false;
        }
        else if ((bool)disableWaterAutoRoll.GetProperty() && __instance._fluidType == FluidVolume.Type.WATER)
        {
            __instance._allowShipAutoroll = false;
        }
    }
    #endregion

    #region ThrustModulator
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipThrusterController), nameof(ShipThrusterController.ReadTranslationalInput))]
    public static void LimitTranslationalInput(ShipThrusterController __instance, ref Vector3 __result)
    {
        if ((bool)enableThrustModulator.GetProperty())
        {
            __result *= ShipEnhancements.Instance.ThrustModulatorFactor
                * (SELocator.GetShipOverdriveController().OnCooldown ? SELocator.GetShipOverdriveController().ThrustMultiplier : 1f);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Autopilot), nameof(Autopilot.ReadTranslationalInput))]
    public static bool LimitAutopilotTranslationalInput(Autopilot __instance, ref Vector3 __result)
    {
        if (!(bool)enableThrustModulator.GetProperty()) return true;

        if (__instance._isShipAutopilot && !__instance._shipResources.AreThrustersUsable())
        {
            if (__instance._isMatchingVelocity)
            {
                __instance.StopMatchVelocity();
            }
            else if (__instance._isFlyingToDestination)
            {
                __instance.Abort();
            }
            __result = Vector3.zero;
            return false;
        }

        float multiplier = ShipEnhancements.Instance.ThrustModulatorFactor;

        if (__instance._isMatchingVelocity && __instance._referenceFrame != null)
        {
            float num = Vector3.Distance(__instance._owRigidbody.GetWorldCenterOfMass(), __instance._referenceFrame.GetPosition());
            Vector3 vector;
            if (__instance._referenceFrame.GetAllowMatchAngularVelocity(num))
            {
                vector = __instance._referenceFrame.GetPointVelocity(__instance._owRigidbody.GetCenterOfMass()) - __instance._owRigidbody.GetVelocity();
            }
            else
            {
                vector = __instance._owRigidbody.GetRelativeVelocity(__instance._referenceFrame);
            }
            float magnitude = vector.magnitude;
            float num2 = (__instance._ignoreThrustLimits ? __instance._thrusterModel.GetMaxTranslationalThrust() * multiplier
                : Mathf.Min(__instance._rulesetDetector.GetThrustLimit(), __instance._thrusterModel.GetMaxTranslationalThrust() * multiplier));
            float num3 = num2 * Time.fixedDeltaTime;
            float num4 = num2 / (__instance._thrusterModel.GetMaxTranslationalThrust() * multiplier);
            if (magnitude < num3)
            {
                num4 *= magnitude / num3;
            }
            if (__instance._stopMatchingNextFrame)
            {
                __instance._stopMatchingNextFrame = false;

                var eventDelegate1 = (MulticastDelegate)typeof(Autopilot).GetField("OnArriveAtDestination", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__instance);
                var eventDelegate2 = (MulticastDelegate)typeof(Autopilot).GetField("OnMatchedVelocity", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__instance);
                if (eventDelegate1 != null && __instance._isFlyingToDestination)
                {
                    float num5 = num - __instance._referenceFrame.GetAutopilotArrivalDistance();
                    foreach (var handler in eventDelegate1.GetInvocationList())
                    {
                        handler.Method.Invoke(handler.Target, [num5]);
                    }
                    __instance._isFlyingToDestination = false;
                }
                else if (eventDelegate2 != null)
                {
                    foreach (var handler in eventDelegate2.GetInvocationList())
                    {
                        handler.Method.Invoke(handler.Target, null);
                    }
                }

                __instance.StopMatchVelocity();
            }
            else
            {
                float num6 = magnitude - num3 * num4;
                float num7 = 0.01f;
                ForceDetector attachedForceDetector = __instance._referenceFrame.GetOWRigidBody().GetAttachedForceDetector();
                if (attachedForceDetector != null)
                {
                    num7 = (Vector3.Project(attachedForceDetector.GetForceAcceleration() - __instance._forceDetector.GetForceAcceleration(), vector.normalized) * Time.deltaTime).magnitude;
                }
                if (num6 <= num7)
                {
                    __instance._stopMatchingNextFrame = true;
                    __result = Vector3.zero;
                    return false;
                }
            }
            __result = __instance.transform.InverseTransformDirection(vector.normalized * num4) * multiplier;
            return false;
        }
        if (!__instance._isFlyingToDestination || __instance._referenceFrame == null)
        {
            __result = Vector3.zero;
            return false;
        }
        __instance._isLiningUpDestination = false;
        __instance._isApproachingDestination = false;
        Vector3 vector2 = __instance._referenceFrame.GetPosition() - __instance._owRigidbody.GetWorldCenterOfMass();
        float magnitude2 = vector2.magnitude;
        Vector3 relativeVelocity = __instance._owRigidbody.GetRelativeVelocity(__instance._referenceFrame);
        Vector3 vector3 = Vector3.Project(relativeVelocity, vector2);
        float num8 = vector3.magnitude * -Mathf.Sign(Vector3.Dot(vector2, vector3));
        if (num8 < -1f)
        {
            __instance._isLiningUpDestination = true;
            __result = __instance.transform.InverseTransformDirection(relativeVelocity.normalized) * multiplier;
            return false;
        }
        Vector3 vector4 = Vector3.Project(__instance._forceDetector.GetForceAcceleration(), vector2);
        float num9 = vector4.magnitude * -Mathf.Sign(Vector3.Dot(vector2, vector4));
        Vector3 vector5 = Vector3.Project(__instance._referenceFrame.GetAcceleration(), vector2);
        float num10 = vector5.magnitude * Mathf.Sign(Vector3.Dot(vector2, vector5)) + num9 + (__instance._thrusterModel.GetMaxTranslationalThrust() * multiplier);
        if (Mathf.Pow(Mathf.Abs(num8), 2f) / (2f * num10) > magnitude2 - __instance._referenceFrame.GetAutopilotArrivalDistance())
        {
            var eventDelegate3 = (MulticastDelegate)typeof(Autopilot).GetField("OnFireRetroRockets", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__instance);

            if (eventDelegate3 != null)
            {
                foreach (var handler in eventDelegate3.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, null);
                }
            }
            __instance.StartMatchVelocity(__instance._referenceFrame, true);
            __result = Vector3.zero;
            return false;
        }
        Vector3 vector6 = relativeVelocity - vector3;
        if (vector6.magnitude > (__instance._thrusterModel.GetMaxTranslationalThrust() * multiplier) / 10f)
        {
            __instance._isLiningUpDestination = true;
            if (num8 > 10f)
            {
                vector3 = Vector3.zero;
            }
        }
        else
        {
            __instance._isApproachingDestination = true;
            vector6 *= vector3.magnitude / 2f;
            if (num8 > 0f)
            {
                vector3 *= -1f;
            }
            else if (num8 <= 0f)
            {
                vector6 = Vector3.zero;
                vector3 = vector2;
            }
        }
        __result = __instance.transform.InverseTransformDirection((vector6 + vector3).normalized) * multiplier;
        return false;
    }
    #endregion

    #region TemperatureZone
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SunController), nameof(SunController.UpdateScale))]
    public static void UpdateSunTempZone(SunController __instance, float scale)
    {
        if (!(bool)enableShipTemperature.GetProperty()) return;

        TemperatureZone tempZone = __instance.GetComponentInChildren<TemperatureZone>();
        if (tempZone != null)
        {
            tempZone.SetScale(scale);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SupernovaEffectController), nameof(SupernovaEffectController.FixedUpdate))]
    public static void UpdateSupernovaTempZone(SupernovaEffectController __instance)
    {
        if (!(bool)enableShipTemperature.GetProperty()) return;

        TemperatureZone tempZone = __instance.GetComponentInChildren<TemperatureZone>();
        if (tempZone != null)
        {
            tempZone.SetScale(__instance._currentSupernovaScale);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.SetState))]
    public static void UpdateCampfireTemperatureZone(Campfire __instance)
    {
        if (!(bool)enableShipTemperature.GetProperty()) return;

        __instance.transform.parent.GetComponentInChildren<TemperatureZone>()?.SetVolumeActive(__instance._state == Campfire.State.LIT);
    }



    [HarmonyPostfix]
    [HarmonyPatch(typeof(RulesetDetector), nameof(RulesetDetector.GetThrustLimit))]
    public static void ShipTemperatureThrustEffects(RulesetDetector __instance, ref float __result)
    {
        if (!(bool)enableShipTemperature.GetProperty() || (float)temperatureDifficulty.GetProperty() <= 0f) return;

        if (__instance.CompareTag("ShipDetector") && SELocator.GetShipTemperatureDetector() != null)
        {
            float ratio = SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio();
            if (ratio < 0f)
            {
                float lerp = Mathf.InverseLerp(-0.02f, -0.8f, ratio);
                __result *= 1f - (0.5f * lerp * (float)temperatureDifficulty.GetProperty());
            }
            else
            {
                float lerp = Mathf.InverseLerp(0.02f, 0.8f, ratio);
                __result *= 1f + (0.5f * lerp * (float)temperatureDifficulty.GetProperty());
            }
        }
    }
    #endregion

    #region DisableReferenceFrame
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ReferenceFrameTracker), nameof(ReferenceFrameTracker.Update))]
    public static bool DisableReferenceFrame(ReferenceFrameTracker __instance)
    {
        if ((bool)disableReferenceFrame.GetProperty())
        {
            if (__instance._activeCam == null)
            {
                return false;
            }
            if (__instance._cloakController != null && __instance._hasTarget && !__instance._currentReferenceFrame.GetOWRigidBody().IsKinematic()
                && __instance._cloakController.CheckBodyInsideCloak(__instance._currentReferenceFrame.GetOWRigidBody()) != __instance._cloakController.isPlayerInsideCloak)
            {
                __instance.UntargetReferenceFrame();
            }
            //__instance._playerTargetingActive = Locator.GetPlayerSuit().IsWearingHelmet() && PlayerState.InZeroG() && __instance._blockerCount <= 0;
            //__instance._shipTargetingActive = PlayerState.AtFlightConsole();
            //__instance._mapTargetingActive = __instance._isMapView && (__instance._playerTargetingActive || PlayerState.IsInsideShip());
            __instance._playerTargetingActive = false;
            __instance._shipTargetingActive = false;
            __instance._mapTargetingActive = false;
            if (__instance._playerTargetingActive || __instance._shipTargetingActive || __instance._mapTargetingActive)
            {
                __instance.UpdateTargeting();
            }
            return false;
        }
        else if ((bool)alwaysAllowLockOn.GetProperty())
        {
            if (__instance._activeCam == null)
            {
                return false;
            }
            if (__instance._cloakController != null && __instance._hasTarget && !__instance._currentReferenceFrame.GetOWRigidBody().IsKinematic() && __instance._cloakController.CheckBodyInsideCloak(__instance._currentReferenceFrame.GetOWRigidBody()) != __instance._cloakController.isPlayerInsideCloak)
            {
                __instance.UntargetReferenceFrame();
            }
            //__instance._playerTargetingActive = Locator.GetPlayerSuit().IsWearingHelmet() && PlayerState.InZeroG() && __instance._blockerCount <= 0;
            __instance._playerTargetingActive = Locator.GetPlayerSuit().IsWearingHelmet()
                && (PlayerState.InZeroG() || PlayerState.InMapView()) && __instance._blockerCount <= 0;
            __instance._shipTargetingActive = PlayerState.AtFlightConsole();
            __instance._mapTargetingActive = __instance._isMapView && (__instance._playerTargetingActive || PlayerState.IsInsideShip());
            if (__instance._playerTargetingActive || __instance._shipTargetingActive || __instance._mapTargetingActive)
            {
                __instance.UpdateTargeting();
            }

            return false;
        }

        return true;
    }
    #endregion

    #region DisableHUDMarkers
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CanvasMarker), nameof(CanvasMarker.SetVisibility))]
    public static bool DisableHUDMarker(CanvasMarker __instance, bool value)
    {
        if (!(bool)disableMapMarkers.GetProperty()) return true;

        bool tryEnable = value;
        bool isLogMarker = ShipLogEntryHUDMarker.s_entryLocation != null && __instance._visualTarget == ShipLogEntryHUDMarker.s_entryLocation.GetTransform();
        bool logMarkerOutsideCloak = !isLogMarker || !ShipLogEntryHUDMarker.s_entryLocation.IsWithinCloakField();
        bool playerInCloak = Locator.GetCloakFieldController() != null && Locator.GetCloakFieldController().isPlayerInsideCloak;
        bool logMarkerInCloak = isLogMarker && ShipLogEntryHUDMarker.s_entryLocation.IsWithinCloakField();

        if (tryEnable && (logMarkerOutsideCloak || (playerInCloak && logMarkerInCloak)))
        {
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapMarker), nameof(MapMarker.Start))]
    public static void DisableMapMarker(MapMarker __instance)
    {
        if (!(bool)disableMapMarkers.GetProperty() || !__instance) return;

        bool isLogMarker = __instance.GetComponent<ShipLogEntryHUDMarker>() != null;
        bool playerInCloak = Locator.GetCloakFieldController() != null && Locator.GetCloakFieldController().isPlayerInsideCloak;
        bool markerInCloak = ShipLogEntryHUDMarker.s_entryLocation != null && ShipLogEntryHUDMarker.s_entryLocation.IsWithinCloakField();

        if (isLogMarker && !playerInCloak && markerInCloak)
        {
            return;
        }

        __instance.DisableMarker();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapMarker), nameof(MapMarker.EnableMarker))]
    public static bool CancelMapMarkerEnable(MapMarker __instance)
    {
        if (!(bool)disableMapMarkers.GetProperty() || !__instance) return true;

        bool isLogMarker = __instance.GetComponent<ShipLogEntryHUDMarker>() != null;
        bool playerInCloak = Locator.GetCloakFieldController() != null && Locator.GetCloakFieldController().isPlayerInsideCloak;
        bool markerInCloak = ShipLogEntryHUDMarker.s_entryLocation != null && ShipLogEntryHUDMarker.s_entryLocation.IsWithinCloakField();

        if (isLogMarker && !playerInCloak && markerInCloak)
        {
            return true;
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipLogEntryHUDMarker), nameof(ShipLogEntryHUDMarker.RefreshOwnVisibility))]
    public static bool DisableInsideCloak(ShipLogEntryHUDMarker __instance)
    {
        if (!(bool)disableMapMarkers.GetProperty()) return true;

        bool markerInCloak = ShipLogEntryHUDMarker.s_entryLocation != null && ShipLogEntryHUDMarker.s_entryLocation.IsWithinCloakField();
        bool playerInCloak = Locator.GetCloakFieldController() != null && Locator.GetCloakFieldController().isPlayerInsideCloak;

        if (playerInCloak && markerInCloak)
        {
            __instance._isVisible = false;
            __instance._canvasMarker.SetVisibility(false);
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlaneOffsetMarker), nameof(PlaneOffsetMarker.Awake))]
    public static void HideMapOffsetMarker(PlaneOffsetMarker __instance)
    {
        __instance._lineColor = new Color(__instance._lineColor.r, __instance._lineColor.g, __instance._lineColor.b, 0f);
        __instance._gridColor = new Color(__instance._gridColor.r, __instance._gridColor.g, __instance._gridColor.b, 0f);
    }
    #endregion

    #region AutoHatch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(HatchController), nameof(HatchController.OpenHatch))]
    public static void ActivateHatchTractorBeam(HatchController __instance, bool __runOriginal)
    {
        if (!__runOriginal) return;

        if ((bool)extraNoise.GetProperty())
        {
            SELocator.GetShipTransform().GetComponentInChildren<ShipNoiseMaker>()._noiseRadius += 100f;
        }

        if (!(bool)enableAutoHatch.GetProperty() || ShipEnhancements.InMultiplayer) return;

        ShipTractorBeamSwitch beamSwitch = SELocator.GetShipBody().GetComponentInChildren<ShipTractorBeamSwitch>();

        if (!__instance.IsPlayerInShip() && beamSwitch._functional)
        {
            beamSwitch.ActivateTractorBeam();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipTractorBeamSwitch), nameof(ShipTractorBeamSwitch.OnTriggerExit))]
    public static bool CloseHatchOutsideShip(ShipTractorBeamSwitch __instance)
    {
        if (!(bool)enableAutoHatch.GetProperty() || ShipEnhancements.InMultiplayer || (bool)disableHatch.GetProperty()) return true;

        HatchController hatch = SELocator.GetShipBody().GetComponentInChildren<HatchController>();
        if (!hatch._hatchObject.activeInHierarchy)
        {
            hatch.CloseHatch();
            __instance.transform.parent.GetComponentInChildren<AutoHatchController>().EnableInteraction();
            if (__instance._beamFluid._triggerVolume._active)
            {
                __instance.DeactivateTractorBeam();
            }
        }
        return false;
    }
    #endregion

    #region TankDrainMultipliers
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipOxygenTankComponent), nameof(ShipOxygenTankComponent.Update))]
    public static bool DamagedOxygenTankDrain(ShipOxygenTankComponent __instance)
    {
        if (__instance._damaged)
        {
            __instance._shipResources.DrainOxygen(__instance._oxygenLeakRate * Time.deltaTime * (float)oxygenTankDrainMultiplier.GetProperty());
            return false;
        }
        __instance.enabled = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipFuelTankComponent), nameof(ShipFuelTankComponent.Update))]
    public static bool DamagedOxygenTankDrain(ShipFuelTankComponent __instance)
    {
        if (__instance._damaged)
        {
            __instance._shipResources.DrainFuel(__instance._fuelLeakRate * Time.deltaTime * (float)fuelTankDrainMultiplier.GetProperty());
            return false;
        }
        __instance.enabled = false;
        return false;
    }
    #endregion

    #region FixTankEffects
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DamageEffect), nameof(DamageEffect.OnEnable))]
    public static void TryDisableFuelOxygenTankEffects(DamageEffect __instance)
    {
        bool oxygen = __instance.GetComponent<ShipOxygenTankComponent>() != null;
        bool fuel = __instance.GetComponent<ShipFuelTankComponent>() != null;

        if (oxygen && (ShipEnhancements.Instance.oxygenDepleted || (bool)disableShipOxygen.GetProperty()))
        {
            __instance._particleSystem.Stop();
            __instance._particleAudioSource.Stop();
        }
        else if (fuel && ShipEnhancements.Instance.fuelDepleted)
        {
            __instance._particleSystem.Stop();
            __instance._particleAudioSource.Stop();
        }
    }
    #endregion

    #region AngularDrag
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ThrusterModel), nameof(ThrusterModel.Awake))]
    public static void InitAngularDrag(ThrusterModel __instance)
    {
        if (!__instance.gameObject.CompareTag("Ship")) return;

        __instance._angularDrag *= (float)spaceAngularDragMultiplier.GetProperty();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ThrusterModel), nameof(ThrusterModel.Awake))]
    public static void RemoveRotationSpeedLimit(ThrusterModel __instance)
    {
        if (!__instance.gameObject.CompareTag("Ship")) return;

        if ((bool)disableRotationSpeedLimit.GetProperty())
        {
            __instance._owRigidbody.SetMaxAngularVelocity(25f);
            // Changing the max velocity makes the ship more sensitive
            __instance._maxRotationalThrust *= 0.75f;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ThrusterModel), nameof(ThrusterModel.FireRotationalThrusters))]
    public static bool RemoveRotationLimit(ThrusterModel __instance)
    {
        if (!(bool)disableRotationSpeedLimit.GetProperty() || !__instance.gameObject.CompareTag("Ship")) return true;

        __instance._localAngularAcceleration = __instance._rotationalInput * __instance._maxRotationalThrust;
        if (__instance._localAngularAcceleration.sqrMagnitude <= 0f)
        {
            __instance._isRotationalFiring = false;
            return false;
        }
        __instance._isRotationalFiring = true;
        if (__instance._usePhysicsToRotate)
        {
            __instance._owRigidbody.AddLocalAngularAcceleration(__instance._localAngularAcceleration);
            return false;
        }
        __instance._manualAngularVelocity += __instance.transform.TransformDirection(__instance._localAngularAcceleration * Time.fixedDeltaTime);

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FluidDetector), nameof(FluidDetector.AddAngularDrag))]
    public static void RemoveFluidAngularDrag(FluidDetector __instance, FluidVolume fluidVolume)
    {
        // Only runs when in fluid
        if (__instance is ShipFluidDetector && fluidVolume is SimpleFluidVolume
            && (fluidVolume._fluidType == FluidVolume.Type.AIR || fluidVolume._fluidType == FluidVolume.Type.FOG))
        {
            __instance._netAngularAcceleration *= (float)atmosphereAngularDragMultiplier.GetProperty();
        }
    }
    #endregion

    #region Scout
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.RetrieveProbe))]
    public static bool DisableProbeRetrieve(ProbeLauncher __instance, bool forcedRetrieval)
    {
        if (ShipEnhancements.InMultiplayer)
        {
            return true;
        }

        return CanRetrieveProbe(__instance, forcedRetrieval);
    }

    public static bool CanRetrieveProbe(ProbeLauncher __instance, bool forcedRetrieval)
    {
        bool manualScoutRecall = (bool)enableManualScoutRecall.GetProperty();
        bool scoutLauncherComponent = (bool)enableScoutLauncherComponent.GetProperty();
        bool recallDisabled = (bool)disableScoutRecall.GetProperty();
        bool launchingDisabled = (bool)disableScoutLaunching.GetProperty();
        bool playerLauncher = __instance.GetName() == ProbeLauncher.Name.Player;
        bool shipLauncher = __instance.GetName() == ProbeLauncher.Name.Ship;
        bool usingShip = PlayerState.AtFlightConsole();

        bool allSettingsFalse = !manualScoutRecall
            && !scoutLauncherComponent
            && !recallDisabled
            && !launchingDisabled;
        bool playerOrShipLauncher = playerLauncher || shipLauncher;

        if (allSettingsFalse || !playerOrShipLauncher)
        {
            return true;
        }

        if (ShipEnhancements.Instance.probeDestroyed)
        {
            return false;
        }

        if (playerLauncher)
        {
            if (recallDisabled && !manualScoutRecall && ShipProbePickupVolume.probeInShip)
            {
                return true;
            }
            if (manualScoutRecall && !ProbePickupVolume.canRetrieveProbe)
            {
                return false;
            }
        }
        if (shipLauncher)
        {
            if (recallDisabled && usingShip)
            {
                return false;
            }
            if (scoutLauncherComponent && SELocator.GetProbeLauncherComponent().isDamaged)
            {
                return false;
            }
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.LaunchProbe))]
    public static bool DisableProbeLaunch(ProbeLauncher __instance)
    {
        bool manualScoutRecall = (bool)enableManualScoutRecall.GetProperty();
        bool scoutLauncherComponent = (bool)enableScoutLauncherComponent.GetProperty();
        bool recallDisabled = (bool)disableScoutRecall.GetProperty();
        bool launchingDisabled = (bool)disableScoutLaunching.GetProperty();
        bool playerLauncher = __instance.GetName() == ProbeLauncher.Name.Player;
        bool shipLauncher = __instance.GetName() == ProbeLauncher.Name.Ship;
        bool usingShip = PlayerState.AtFlightConsole();

        bool allSettingsFalse = !manualScoutRecall
            && !scoutLauncherComponent
            && !recallDisabled
            && !launchingDisabled;
        bool playerOrShipLauncher = playerLauncher || shipLauncher;

        if (allSettingsFalse || !playerOrShipLauncher)
        {
            return true;
        }

        if (ShipEnhancements.Instance.probeDestroyed)
        {
            return false;
        }

        if (playerLauncher)
        {
            if ((manualScoutRecall || recallDisabled) && ShipProbePickupVolume.probeInShip)
            {
                ShipNotifications.PostScoutInShipNotification();
                return false;
            }
        }
        if (shipLauncher)
        {
            if (recallDisabled && !ShipProbePickupVolume.probeInShip)
            {
                ShipNotifications.PostScoutLauncherEmptyNotification();
                return false;
            }
            if (launchingDisabled && usingShip)
            {
                return false;
            }
            if (scoutLauncherComponent && SELocator.GetProbeLauncherComponent().isDamaged)
            {
                return false;
            }
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.OnForceRetrieveProbe))]
    public static bool FixForceRetrieveProbe(ProbeLauncher __instance)
    {
        if (!(bool)enableManualScoutRecall.GetProperty() && !(bool)disableScoutRecall.GetProperty()
            && !(bool)enableScoutLauncherComponent.GetProperty())
        {
            return true;
        }

        bool flag = false;

        if ((bool)disableScoutRecall.GetProperty())
        {
            flag = true;
        }
        else if ((bool)enableScoutLauncherComponent.GetProperty())
        {
            if (SELocator.GetProbeLauncherComponent().isDamaged)
            {
                flag = true;
            }
        }

        if ((bool)enableManualScoutRecall.GetProperty() && flag) return false;

        // This recalls to the player launcher
        if (__instance.GetName() == ProbeLauncher.Name.Player && flag)
        {
            __instance._activeProbe = SELocator.GetProbe();
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.OnForceRetrieveProbe))]
    public static bool FixSilentForceRetrieveProbe(ProbeLauncher __instance)
    {
        if (!(bool)enableManualScoutRecall.GetProperty() && !(bool)disableScoutRecall.GetProperty()
            && !(bool)enableScoutLauncherComponent.GetProperty())
        {
            return true;
        }

        bool flag = false;

        if ((bool)disableScoutRecall.GetProperty())
        {
            flag = true;
        }
        else if ((bool)enableScoutLauncherComponent.GetProperty())
        {
            if (SELocator.GetProbeLauncherComponent().isDamaged)
            {
                flag = true;
            }
        }

        if ((bool)enableManualScoutRecall.GetProperty() && flag) return false;

        // This recalls to the player launcher
        if (__instance.GetName() == ProbeLauncher.Name.Player && flag)
        {
            __instance._activeProbe = SELocator.GetProbe();
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(PlayerTool), nameof(PlayerTool.Update))]
    public static void PlayerTool_Update(PlayerTool __instance) { }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.Update))]
    public static bool ShowConnectionLostNotification(ProbeLauncher __instance)
    {
        if (!(bool)enableManualScoutRecall.GetProperty()
            || (__instance.GetName() != ProbeLauncher.Name.Player && __instance.GetName() != ProbeLauncher.Name.Ship)) return true;

        PlayerTool_Update(__instance);
        __instance.enabled = true;
        if (!__instance.AllowInput())
        {
            return true;
        }
        if (__instance._isEquipped && ShipEnhancements.Instance.probeDestroyed)
        {
            if (OWInput.IsNewlyPressed(InputLibrary.probeLaunch, InputMode.All)
                || OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary, InputMode.All)
                || OWInput.IsNewlyPressed(InputLibrary.toolActionSecondary, InputMode.All))
            {
                NotificationData notificationData = new NotificationData(UITextLibrary.GetString(UITextType.NotificationUnableToRetrieveProbe));
                NotificationManager.SharedInstance.PostNotification(notificationData, false);
                Locator.GetPlayerAudioController().PlayNegativeUISound();
            }
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SingularityWarpEffect), nameof(SingularityWarpEffect.WarpObjectIn))]
    public static bool SkipWarpInEffect(SingularityWarpEffect __instance)
    {
        if ((!(bool)enableManualScoutRecall.GetProperty() && !(bool)disableScoutRecall.GetProperty())
           || !ProbePickupVolume.canRetrieveProbe)
        {
            return true;
        }

        PlayerProbeLauncher launcher = __instance.GetComponentInParent<PlayerProbeLauncher>();
        if (launcher != null)
        {
            if (launcher.GetName() == ProbeLauncher.Name.Player && !__instance.transform.parent.gameObject.activeInHierarchy)
            {
                launcher._preLaunchProbeProxy.transform.localScale = Vector3.one;
                return false;
            }
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SingularityWarpEffect), nameof(SingularityWarpEffect.WarpObjectOut))]
    public static bool SkipWarpOutEffect(SingularityWarpEffect __instance)
    {
        if ((!(bool)enableManualScoutRecall.GetProperty() && !(bool)disableScoutRecall.GetProperty())
           || !ShipProbePickupVolume.canTransferProbe)
        {
            return true;
        }

        PlayerProbeLauncher launcher = __instance.GetComponentInParent<PlayerProbeLauncher>();
        if (launcher != null)
        {
            if (launcher.GetName() == ProbeLauncher.Name.Player && !__instance.transform.parent.gameObject.activeInHierarchy)
            {
                launcher._preLaunchProbeProxy.transform.localScale = Vector3.one;
                return false;
            }
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.SetActiveProbe))]
    public static bool KeepProbeHidden(ProbeLauncher __instance)
    {
        if (!(bool)enableManualScoutRecall.GetProperty() ||
            (__instance.GetName() != ProbeLauncher.Name.Player && __instance.GetName() != ProbeLauncher.Name.Ship)) return true;

        if (ShipEnhancements.Instance.probeDestroyed) return false;

        if (__instance.GetName() != ProbeLauncher.Name.Player) return true;

        ShipProbeLauncherEffects launcherEffects = __instance.GetComponent<ShipProbeLauncherEffects>();
        if (launcherEffects != null && launcherEffects.componentDamaged)
        {
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProbePromptController), nameof(ProbePromptController.Update))]
    public static void FixProbePrompts(ProbePromptController __instance)
    {
        bool manualScoutRecall = (bool)enableManualScoutRecall.GetProperty();
        bool scoutLauncherComponent = (bool)enableScoutLauncherComponent.GetProperty();
        bool recallDisabled = (bool)disableScoutRecall.GetProperty();
        bool launchingDisabled = (bool)disableScoutLaunching.GetProperty();
        bool shipPhotoMode = (bool)scoutPhotoMode.GetProperty();
        bool playerLauncher = __instance._activeLauncher.GetName() == ProbeLauncher.Name.Player;
        bool shipLauncher = __instance._activeLauncher.GetName() == ProbeLauncher.Name.Ship;
        bool usingShip = PlayerState.AtFlightConsole();

        bool allSettingsFalse = !manualScoutRecall
            && !scoutLauncherComponent
            && !recallDisabled
            && !launchingDisabled;

        if (allSettingsFalse)
        {
            return;
        }

        bool canRecall = true;
        bool canLaunch = true;
        bool canSnapshot = false;

        if (usingShip)
        {
            bool damaged = scoutLauncherComponent && SELocator.GetProbeLauncherComponent().isDamaged;
            if (recallDisabled || damaged)
            {
                canRecall = false;
            }
            if (launchingDisabled || damaged)
            {
                canLaunch = false;
            }
            if (recallDisabled && !ShipProbePickupVolume.probeInShip)
            {
                canLaunch = false;
            }
            if (!shipPhotoMode || ((!recallDisabled || ShipProbePickupVolume.probeInShip) && !damaged))
            {
                canSnapshot = true;
            }
        }
        else
        {
            canSnapshot = true;
            if (manualScoutRecall)
            {
                canRecall = false;
            }

            if (ShipProbePickupVolume.probeInShip)
            {
                canLaunch = false;
                __instance._retrievePrompt.SetVisibility(!manualScoutRecall);
            }
        }

        if (canRecall)
        {
            __instance._retrievePrompt.SetDisplayState(ScreenPrompt.DisplayState.Normal);
        }
        else
        {
            __instance._retrievePrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
            __instance._retrieveCenterPrompt.SetVisibility(false);
        }

        if (canLaunch)
        {
            __instance._launchPrompt.SetDisplayState(ScreenPrompt.DisplayState.Normal);
        }
        else
        {
            __instance._launchPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
        }

        if (canSnapshot)
        {
            __instance._takeSnapshotPrompt.SetDisplayState(ScreenPrompt.DisplayState.Normal);
        }
        else
        {
            __instance._takeSnapshotPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
            __instance._snapshotCenterPrompt.SetVisibility(false);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.UpdatePreLaunch))]
    public static void PreLaunchRecall(ProbeLauncher __instance)
    {
        bool recallOrLaunchingDisabled = (bool)disableScoutRecall.GetProperty() || (bool)disableScoutLaunching.GetProperty();
        bool manualScoutRecall = (bool)enableManualScoutRecall.GetProperty();
        bool playerLauncher = __instance.GetName() == ProbeLauncher.Name.Player;
        bool usingShip = PlayerState.AtFlightConsole();

        if (recallOrLaunchingDisabled && playerLauncher && !manualScoutRecall
            && !usingShip && ShipProbePickupVolume.probeInShip)
        {
            bool flag = InputLibrary.toolActionPrimary.HasSameBinding(InputLibrary.probeRetrieve, OWInput.UsingGamepad());
            if ((flag && OWInput.IsPressed(InputLibrary.probeRetrieve, 0.5f)) || (!flag && OWInput.IsNewlyPressed(InputLibrary.probeRetrieve, InputMode.All)))
            {
                SELocator.GetShipTransform().GetComponentInChildren<ShipProbePickupVolume>().RecallProbeFromShip();
                return;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.AllowPhotoMode))]
    public static void UpdateAllowPhotoMode(ProbeLauncher __instance, ref bool __result)
    {
        if (__instance.GetName() == ProbeLauncher.Name.Player)
        {
            if (__result && ShipProbePickupVolume.probeInShip)
            {
                __result = false;
            }
        }
        else if (__instance.GetName() == ProbeLauncher.Name.Ship && (bool)scoutPhotoMode.GetProperty())
        {
            __result = !(bool)disableScoutRecall.GetProperty() || ShipProbePickupVolume.probeInShip;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.UpdatePreLaunch))]
    public static bool DisablePhotoMode(ProbeLauncher __instance)
    {
        bool recallOrLaunchingDisabled = (bool)disableScoutRecall.GetProperty() || (bool)disableScoutLaunching.GetProperty();
        bool manualScoutRecall = (bool)enableManualScoutRecall.GetProperty();

        if (!recallOrLaunchingDisabled && !manualScoutRecall)
        {
            return true;
        }

        if (__instance.GetName() == ProbeLauncher.Name.Player
            && (recallOrLaunchingDisabled || manualScoutRecall) && ShipProbePickupVolume.probeInShip)
        {
            if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary, InputMode.All))
            {
                if (__instance.InPhotoMode())
                {
                    if (__instance._launcherGeometry != null)
                    {
                        __instance._launcherGeometry.SetActive(false);
                    }
                    __instance.TakeSnapshotWithCamera(__instance._preLaunchCamera);
                    if (__instance._launcherGeometry != null)
                    {
                        __instance._launcherGeometry.SetActive(true);
                        return false;
                    }
                }
                else if (__instance.AllowLaunchMode())
                {
                    __instance.LaunchProbe();
                }
            }
            return false;
        }
        else if (__instance.GetName() == ProbeLauncher.Name.Ship)
        {
            if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary, InputMode.All))
            {
                if (__instance.InPhotoMode() && (!(bool)disableScoutRecall.GetProperty() || ShipProbePickupVolume.probeInShip)
                    && (SELocator.GetProbeLauncherComponent() == null || !SELocator.GetProbeLauncherComponent().isDamaged))
                {
                    if (__instance._launcherGeometry != null)
                    {
                        __instance._launcherGeometry.SetActive(false);
                    }
                    __instance.TakeSnapshotWithCamera(__instance._preLaunchCamera);
                    if (__instance._launcherGeometry != null)
                    {
                        __instance._launcherGeometry.SetActive(true);
                        return false;
                    }
                }
                else if (__instance.AllowLaunchMode())
                {
                    __instance.LaunchProbe();
                }
            }
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SurveyorProbe), nameof(SurveyorProbe.Deactivate))]
    public static bool DisableEventOnProbeDeactivate(SurveyorProbe __instance)
    {
        if (!ShipEnhancements.Instance.probeDestroyed) return true;

        if (__instance._lightSourceVol != null)
        {
            __instance._lightSourceVol.SetVolumeActivation(false);
        }
        __instance._hudMarker.DestroyFogMarkersOnRetrieve();
        __instance._hudMarker.MarkAsRetrieved();
        Locator.GetMarkerManager().RequestFogMarkerUpdate();
        __instance.transform.parent = null;
        __instance.transform.localScale = Vector3.one;
        __instance._rotatingCam.ResetRotation();
        __instance._forwardCam.SetSandLevelController(null);
        __instance._reverseCam.SetSandLevelController(null);
        __instance._rotatingCam.SetSandLevelController(null);
        __instance._detectorCollider.SetActivation(false);
        __instance._detectorShape.SetActivation(false);
        __instance.gameObject.SetActive(false);
        __instance._isRetrieving = false;
        return false;
    }
    #endregion

    #region ShipItemPlacing
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemTool), nameof(ItemTool.UpdateIsDroppable))]
    public static bool DropItemsInShip(ItemTool __instance, out RaycastHit hit, out OWRigidbody targetRigidbody, out IItemDropTarget dropTarget, ref bool __result)
    {
        hit = default(RaycastHit);
        targetRigidbody = null;
        dropTarget = null;

        bool unrestricted = ShipEnhancements.ExperimentalSettings?.UnrestrictedItemPlacement ?? false;

        if (!unrestricted
            && !(bool)enableShipItemPlacement.GetProperty() && !(bool)addTether.GetProperty()
            && !(bool)enableRemovableGravityCrystal.GetProperty()) return true;

        bool isTether = (bool)addTether.GetProperty() && __instance.GetHeldItemType() == ShipEnhancements.Instance.TetherHookType;
        bool isGravityCrystal = __instance.GetHeldItemType() == ShipEnhancements.Instance.GravityCrystalType;
        bool shipItemPlacement = (bool)enableShipItemPlacement.GetProperty();

        PlayerCharacterController playerController = Locator.GetPlayerController();
        bool groundedCheck = !playerController.IsGrounded() && !isTether && !isGravityCrystal && !unrestricted;
        bool insideShipCheck = PlayerState.IsInsideShip() && !unrestricted && (!shipItemPlacement || isTether || isGravityCrystal);

        if (groundedCheck || PlayerState.IsAttached() || insideShipCheck)
        {
            __result = false;
            return false;
        }
        if (__instance._heldItem != null && !__instance._heldItem.CheckIsDroppable())
        {
            __result = false;
            return false;
        }

        bool speedCheck = playerController.GetRelativeGroundVelocity().sqrMagnitude
            >= playerController.GetRunSpeedMagnitude() * playerController.GetRunSpeedMagnitude()
            && !unrestricted && !isTether && !isGravityCrystal;

        if (speedCheck)
        {
            __result = false;
            return false;
        }
        Vector3 forward = Locator.GetPlayerTransform().forward;
        Vector3 forward2 = Locator.GetPlayerCamera().transform.forward;
        float cameraForwardsAngle = Vector3.Angle(forward, forward2);
        float angleRatio = Mathf.InverseLerp(0f, 70f, cameraForwardsAngle);
        float dist = 2.5f;
        if (angleRatio <= 1f)
        {
            dist = Mathf.Lerp(2.5f, 4f, angleRatio);
        }
        if (Physics.Raycast(Locator.GetPlayerCamera().transform.position, forward2, out hit, dist, OWLayerMask.physicalMask | OWLayerMask.interactMask))
        {
            if (OWLayerMask.IsLayerInMask(hit.collider.gameObject.layer, OWLayerMask.interactMask))
            {
                __result = false;
                return false;
            }
            float maxSlopeAngle = (unrestricted || isTether || isGravityCrystal) ? 360f : __instance._maxDroppableSlopeAngle;

            if (Vector3.Angle(Locator.GetPlayerTransform().up, hit.normal) <= maxSlopeAngle)
            {
                IgnoreCollision component = hit.collider.GetComponent<IgnoreCollision>();
                if (component == null || !component.PreventsItemDrop())
                {
                    targetRigidbody = hit.collider.GetAttachedOWRigidbody(false);
                    if (targetRigidbody.gameObject.CompareTag("Ship") && !unrestricted && !shipItemPlacement && !isTether)
                    {
                        return false;
                    }
                    dropTarget = hit.collider.GetComponentInParent<IItemDropTarget>();
                    __result = true;
                    return false;
                }
            }
        }
        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemTool), nameof(ItemTool.DropItem))]
    public static bool ParentItemsToShipModules(ItemTool __instance,
        RaycastHit hit, OWRigidbody targetRigidbody, IItemDropTarget customDropTarget)
    {
        if (customDropTarget != null || ShipEnhancements.InMultiplayer) return true;

        // Get module to parent
        Transform module = hit.collider.GetComponentInParent<ShipDetachableModule>()?.transform;
        if (module == null)
        {
            module = hit.collider.GetComponentInParent<ShipDetachableLeg>()?.transform;
            if (module == null) return true;
        }

        // Regular code
        Locator.GetPlayerAudioController().PlayDropItem(__instance._heldItem.GetItemType());
        GameObject gameObject = hit.collider.gameObject;
        ISectorGroup sectorGroup = gameObject.GetComponent<ISectorGroup>();
        Sector sector = null;
        while (sectorGroup == null && gameObject.transform.parent != null)
        {
            gameObject = gameObject.transform.parent.gameObject;
            sectorGroup = gameObject.GetComponent<ISectorGroup>();
        }
        if (sectorGroup != null)
        {
            sector = sectorGroup.GetSector();
            if (sector == null && sectorGroup is SectorCullGroup)
            {
                SectorProxy controllingProxy = (sectorGroup as SectorCullGroup).GetControllingProxy();
                if (controllingProxy != null)
                {
                    sector = controllingProxy.GetSector();
                }
            }
        }

        // Cancel normal parenting
        //Transform transform = ((customDropTarget == null) ? targetRigidbody.transform : customDropTarget.GetItemDropTargetTransform(hit.collider.gameObject));

        // Normal code
        __instance._heldItem.DropItem(hit.point, hit.normal, module, sector, customDropTarget);

        // Never custom dropped target in ship
        /*if (customDropTarget != null)
        {
            customDropTarget.AddDroppedItem(hit.collider.gameObject, __instance._heldItem);
        }*/

        // Normal code
        __instance._heldItem = null;
        Locator.GetToolModeSwapper().UnequipTool();

        return false;
    }

    #endregion

    #region PortableCampfire
    public static float baseRoastingStickMaxZ = 0f;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.Update))]
    public static void SetExtinguishPromptVisibility(Campfire __instance)
    {
        if (!(bool)addPortableCampfire.GetProperty()) return;

        PortableCampfire campfire = (__instance is PortableCampfire) ? (PortableCampfire)__instance : null;
        if (campfire)
        {
            campfire.SetPromptVisibility(false);
            if (campfire._interactVolumeFocus && !campfire._isPlayerSleeping
                && !campfire._isPlayerRoasting && OWInput.IsInputMode(InputMode.Character)
                && Locator.GetToolModeSwapper().GetToolMode() == ToolMode.None)
            {
                campfire.SetPromptVisibility(true);
                campfire.UpdatePrompt();
                if (OWInput.IsNewlyPressed(InputLibrary.cancel, InputMode.All))
                {
                    campfire.OnExtinguishInteract();
                    if (ShipEnhancements.InMultiplayer)
                    {
                        foreach (uint id in ShipEnhancements.PlayerIDs)
                        {
                            ShipEnhancements.QSBCompat.SendCampfireExtinguishState(id, campfire.GetItem());
                        }
                    }
                }
            }
            if (campfire._interactVolumeFocus && campfire._state == Campfire.State.LIT && !campfire._isPlayerRoasting && OWInput.IsInputMode(InputMode.Character))
            {
                campfire._interactVolume._screenPrompt.SetDisplayState(
                    Locator.GetPlayerController().IsGrounded() ? ScreenPrompt.DisplayState.Normal : ScreenPrompt.DisplayState.GrayedOut);
            }
            if (campfire._canSleepHere && campfire._interactVolumeFocus && !campfire._isPlayerSleeping
                && !campfire._isPlayerRoasting && OWInput.IsInputMode(InputMode.Character))
            {
                campfire._sleepPrompt.SetDisplayState(campfire.CanSleepHereNow() && Locator.GetPlayerController().IsGrounded()
                    ? ScreenPrompt.DisplayState.Normal : ScreenPrompt.DisplayState.GrayedOut);
            }
            campfire.UpdateCampfire();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.OnGainFocus))]
    public static void AddExtinguishPrompt(Campfire __instance)
    {
        if (!(bool)addPortableCampfire.GetProperty()) return;

        PortableCampfire campfire = (__instance is PortableCampfire) ? (PortableCampfire)__instance : null;
        if (campfire)
        {
            UpdateFocusedItems(true);
            Locator.GetPromptManager().AddScreenPrompt(campfire.GetPrompt(), PromptPosition.Center, false);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.OnLoseFocus))]
    public static void RemoveExtinguishPrompt(Campfire __instance)
    {
        if (!(bool)addPortableCampfire.GetProperty()) return;

        PortableCampfire campfire = (__instance is PortableCampfire) ? (PortableCampfire)__instance : null;
        if (campfire)
        {
            UpdateFocusedItems(false);
            Locator.GetPromptManager().RemoveScreenPrompt(campfire.GetPrompt(), PromptPosition.Center);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.StartRoasting))]
    public static void RemoveExtinguishPromptWhenRoasting(Campfire __instance)
    {
        if (!(bool)addPortableCampfire.GetProperty()) return;

        PortableCampfire campfire = (__instance is PortableCampfire) ? (PortableCampfire)__instance : null;
        if (campfire)
        {
            campfire.SetPromptVisibility(false);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.StopRoasting))]
    public static void AddExtinguishPromptWhenStopRoasting(Campfire __instance)
    {
        if (!(bool)addPortableCampfire.GetProperty()) return;

        PortableCampfire campfire = (__instance is PortableCampfire) ? (PortableCampfire)__instance : null;
        if (campfire)
        {
            campfire.SetPromptVisibility(true);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.SetState))]
    public static bool FixPortableCampfireNRE(Campfire __instance, Campfire.State newState, bool forceStateUpdate)
    {
        if (__instance is PortableCampfire)
        {
            if (__instance._hazardVolume._triggerVolume == null)
            {
                ShipEnhancements.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() => __instance.SetState(newState, forceStateUpdate));
                return false;
            }
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.SetState))]
    public static void UpdateExtinguished(Campfire __instance, Campfire.State newState)
    {
        if (!(bool)addPortableCampfire.GetProperty()) return;

        PortableCampfire campfire = (__instance is PortableCampfire) ? (PortableCampfire)__instance : null;
        if (campfire)
        {
            if (newState == Campfire.State.UNLIT)
            {
                campfire.SetExtinguished(true);
            }
            else
            {
                campfire.SetExtinguished(false);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RoastingStickController), nameof(RoastingStickController.Awake))]
    public static void SetBaseRoastingStickMaxZ(RoastingStickController __instance)
    {
        baseRoastingStickMaxZ = __instance._stickMaxZ;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Campfire), nameof(Campfire.OnPressInteract))]
    public static bool CancelLightWhenInWater(Campfire __instance)
    {
        if (__instance is PortableCampfire)
        {
            if (!(__instance as PortableCampfire).IsOutsideWater())
            {
                __instance._interactVolume.ResetInteraction();
                return false;
            }
        }
        return true;
    }
    #endregion

    #region ExplosionMultiplier
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExplosionController), nameof(ExplosionController.Update))]
    public static bool FixExplosionHeatVolume(ExplosionController __instance)
    {
        if ((float)shipExplosionMultiplier.GetProperty() == 1f || (float)shipExplosionMultiplier.GetProperty() <= 0f) return true;

        if (!__instance._playing)
        {
            __instance.enabled = false;
            return false;
        }
        __instance.transform.rotation = Quaternion.identity;
        __instance._timer += Time.deltaTime;
        float num = Mathf.Clamp01(__instance._timer / __instance._length);
        float num2 = (num - 2f) * -num;
        __instance._matPropBlock.SetFloat(__instance._propID_ExplosionTime, num2);
        __instance._renderer.SetPropertyBlock(__instance._matPropBlock);
        __instance._light.intensity = __instance._lightIntensity * (1f - num);
        __instance._light.range = __instance._lightRadius * Mathf.Clamp01(num * 10f);
        SphereCollider col = __instance.GetComponent<SphereCollider>();
        col.radius = Mathf.Lerp(0.1f, 1f, num * 2f);

        if (__instance._audioController._loudShipSource.transform.Find("ShipExplosionExpandAudio"))
        {
            OWAudioSource audio = __instance._audioController._loudShipSource.transform
                .Find("ShipExplosionExpandAudio").GetComponent<OWAudioSource>();
            float dist = Vector3.Distance(__instance.transform.position,
                Locator.GetPlayerCamera().transform.position) - (__instance.transform.localScale.x * col.radius);
            float multiplierLerp = Mathf.InverseLerp(20f, 1000f, __instance.transform.localScale.x * col.radius);
            float distLerp = Mathf.InverseLerp(Mathf.Lerp(20f, 1000f, multiplierLerp), 0f, dist);
            float timeLerp1 = Mathf.InverseLerp(0f, 3f, __instance._timer);
            float timeLerp2 = Mathf.InverseLerp(__instance._length / 2f - 10f, __instance._length / 2f, __instance._timer);
            float timeLerp = Mathf.Max(timeLerp1 - timeLerp2, 0);
            audio.SetLocalVolume(distLerp * distLerp * timeLerp);
        }

        if (num > 0.5f)
        {
            __instance._forceVolume.SetVolumeActivation(false);
        }
        if (num == 1f)
        {
            UnityEngine.Object.Destroy(__instance.gameObject);
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExplosionController), nameof(ExplosionController.Play))]
    public static bool PlayBlackHoleExplosion(ExplosionController __instance)
    {
        if ((float)shipExplosionMultiplier.GetProperty() == 0
            || ((float)shipExplosionMultiplier.GetProperty() < 0f && __instance.GetComponentInParent<FuelTankItem>()))
        {
            return false;
        }

        if ((float)shipExplosionMultiplier.GetProperty() >= 100f && !__instance.GetComponentInParent<FuelTankItem>())
        {
            SupernovaEffectController supernovaEffects = SELocator.GetShipTransform().GetComponentInChildren<SupernovaEffectController>(true);
            if (supernovaEffects == null)
            {
                supernovaEffects = GameObject.Find("Module_Engine_Body").GetComponentInChildren<SupernovaEffectController>(true);
            }
            if (supernovaEffects != null)
            {
                GameObject supernovaBody = new GameObject("ShipSupernova_Body");
                supernovaBody.transform.position = supernovaEffects.transform.position;
                OWRigidbody body = supernovaBody.AddComponent<OWRigidbody>();
                body.GetRigidbody().isKinematic = true;
                body.EnableKinematicSimulation();
                body.SetIsTargetable(false);
                body.SetVelocity(supernovaEffects.GetAttachedOWRigidbody().GetVelocity());

                supernovaEffects.transform.parent = GameObject.Find("Sun_Body").transform;
                supernovaEffects.gameObject.SetActive(true);
                supernovaEffects.transform.Find("ExplosionSource").GetComponent<OWAudioSource>().PlayOneShot(AudioType.Sun_Explosion, 1f);
                supernovaEffects.enabled = true;

                supernovaEffects.GetComponentInChildren<ShipSupernovaStreamersController>().OnSupernovaStart(supernovaEffects);
                return false;
            }
        }

        if ((float)shipExplosionMultiplier.GetProperty() >= 10f
            && (float)shipExplosionMultiplier.GetProperty() < 100f)
        {
            __instance._audioController._loudShipSource.transform.Find("ShipExplosionExpandAudio")
                .GetComponent<OWAudioSource>().Play();
        }

        if (__instance is BlackHoleExplosionController)
        {
            BlackHoleExplosionController controller = __instance as BlackHoleExplosionController;
            controller.OpenBlackHole();
            return false;
        }
        else if (__instance.GetComponentInParent<FuelTankItem>())
        {
            __instance._forceVolume.SetVolumeActivation(true);
            if (Vector3.Distance(__instance.transform.position, Locator.GetPlayerTransform().position)
                < __instance.transform.localScale.x * __instance.GetComponent<SphereCollider>().radius)
            {
                RumbleManager.PulseShipExplode();
            }
            __instance._renderer.enabled = true;
            __instance._light.enabled = true;
            /*float num = __instance._audioController.PlayShipExplodeClip();
            if (num > __instance._length)
            {
                __instance._length = num;
            }*/
            __instance._playing = true;
            __instance.enabled = true;
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipAudioController), nameof(ShipAudioController.PlayShipExplodeClip))]
    public static bool AddEffectsToShipExplosionAudio(ShipAudioController __instance, ref float __result)
    {
        if ((float)shipExplosionMultiplier.GetProperty() > 1f)
        {
            OWAudioSource audio = __instance._loudShipSource;
            float dist = Vector3.Distance(audio.transform.position, Locator.GetPlayerTransform().position);
            float cutoffLerp = Mathf.InverseLerp(200f, 10000f, dist);
            audio.GetComponent<AudioLowPassFilter>().cutoffFrequency = Mathf.Lerp(25000f, 300f, Mathf.Pow(cutoffLerp - 1f, 3) + 1f);

            float reverbLerp = Mathf.InverseLerp(200f, 30000f, dist);
            AudioReverbFilter reverb = audio.GetComponent<AudioReverbFilter>();
            reverb.reverbLevel += Mathf.Lerp(0f, 200f, reverbLerp);
            reverb.decayTime += Mathf.Lerp(0f, 3f, reverbLerp);

            float timeLerp = Mathf.InverseLerp(500f, 100000f, dist);
            float delay = Mathf.Lerp(0f, 6f, timeLerp);
            float delayTime = Time.time + delay;

            __result = 5f;

            AudioClip clip;
            if ((float)shipExplosionMultiplier.GetProperty() >= 10f)
            {
                clip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/OW_SP_ShipExploding_LongReverb.ogg");
            }
            else
            {
                clip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/OW_SP_ShipExploding_Long.ogg");
            }

            if (delay > 0f)
            {
                ShipEnhancements.Instance.ModHelper.Events.Unity.RunWhen(() => Time.time > delayTime, () =>
                {
                    __instance._loudShipSource.PlayOneShot(clip, 1f);
                });
            }
            else
            {
                __instance._loudShipSource.PlayOneShot(clip, 1f);
            }
            return false;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ForceVolume), nameof(ForceVolume.CalculateForceAccelerationOnBody))]
    public static void RemoveShipBodyFromForceVolume(ForceVolume __instance, OWRigidbody targetBody, ref Vector3 __result)
    {
        if (__instance.GetComponent<ExplosionController>() && targetBody is ShipBody)
        {
            __result = Vector3.zero;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.OnModuleDetach))]
    public static void ParentExplosionToEngine(ShipDamageController __instance, ShipDetachableModule module)
    {
        if (module.GetComponent<ShipHull>().section == ShipHull.Section.Left)
        {
            if (__instance.GetComponentInChildren<ExplosionController>() != null)
            {
                __instance.GetComponentInChildren<ExplosionController>().transform.parent = module.transform;
            }
        }
    }
    #endregion

    #region InputLatency
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipThrusterController), nameof(ShipThrusterController.ReadTranslationalInput))]
    public static void DelayTranslational(ShipThrusterController __instance, ref Vector3 __result)
    {
        if (InputLatencyController.ReadingSavedInputs && InputLatencyController.IsInputQueued)
        {
            __result = __instance._translationalInput;
        }
        else if ((float)shipInputLatency.GetProperty() > 0f)
        {
            if (__result != Vector3.zero)
            {
                InputLatencyController.AddTranslationalInput(__result);
            }
            __result = InputLatencyController.IsTranslationalInputQueued ? __instance._translationalInput : Vector3.zero;
        }
        else if ((float)shipInputLatency.GetProperty() < 0f && (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost()))
        {
            if (__result != Vector3.zero)
            {
                InputLatencyController.SaveTranslationalInput(__result);
            }
            //__result = InputLatencyController.IsTranslationalInputQueued ? __instance._translationalInput : Vector3.zero;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipThrusterController), nameof(ShipThrusterController.ReadRotationalInput))]
    public static void DelayRotational(ShipThrusterController __instance, ref Vector3 __result)
    {
        if (InputLatencyController.ReadingSavedInputs && InputLatencyController.IsInputQueued)
        {
            __result = __instance._rotationalInput;
        }
        else if ((float)shipInputLatency.GetProperty() > 0f)
        {
            if (__result != Vector3.zero)
            {
                InputLatencyController.AddRotationalInput(__result);
            }
            __result = InputLatencyController.IsRotationalInputQueued ? __instance._rotationalInput : Vector3.zero;
        }
        else if ((float)shipInputLatency.GetProperty() < 0f && (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost()))
        {
            if (__result != Vector3.zero)
            {
                InputLatencyController.SaveRotationalInput(__result);
            }
            //__result = InputLatencyController.IsTranslationalInputQueued ? __instance._translationalInput : Vector3.zero;
        }
    }
    #endregion

    #region EngineSwitch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipResources), nameof(ShipResources.AreThrustersUsable))]
    public static void DisableThrustersWhenEngineOff(ref bool __result)
    {
        if ((bool)enableFragileShip.GetProperty() && ShipEnhancements.Instance.anyPartDamaged)
        {
            __result = false;
        }
        else if ((bool)addEngineSwitch.GetProperty())
        {
            __result = __result && ShipEnhancements.Instance.engineOn;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipThrusterController), nameof(ShipThrusterController.OnEnable))]
    public static bool DisableConsoleEngineIgnition()
    {
        return !(bool)addEngineSwitch.GetProperty();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ElectricalSystem), nameof(ElectricalSystem.SetPowered))]
    public static bool LinkElectricalSystemsToEngine(bool powered)
    {
        return !powered || !(bool)addEngineSwitch.GetProperty() || ShipEnhancements.Instance.engineOn;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ElectricalComponent), nameof(ElectricalComponent.SetPowered))]
    public static bool LinkElectricalComponentsToEngine(bool powered)
    {
        return !powered || !(bool)addEngineSwitch.GetProperty() || ShipEnhancements.Instance.engineOn;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
    public static bool DisableShipEquip(ToolMode mode)
    {
        return !(bool)addEngineSwitch.GetProperty() || ShipEnhancements.Instance.engineOn || !OWInput.IsInputMode(InputMode.ShipCockpit)
            || (mode != ToolMode.Probe && mode != ToolMode.SignalScope);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipAudioController), nameof(ShipAudioController.PlayShipAmbient))]
    public static bool DisableShipAmbience()
    {
        return !(bool)addEngineSwitch.GetProperty() || ShipEnhancements.Instance.engineOn;
    }
    #endregion

    #region IgnitionCancelFix
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.ExitFlightConsole))]
    public static void IgnitionCancelFix()
    {
        ShipThrusterController thrustController = SELocator.GetShipBody().GetComponent<ShipThrusterController>();
        if (thrustController && thrustController._isIgniting)
        {
            thrustController._isIgniting = false;
            GlobalMessenger.FireEvent("CancelShipIgnition");
        }
    }
    #endregion

    #region Overdrive
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipThrusterController), nameof(ShipThrusterController.ReadTranslationalInput))]
    public static bool DisableInputWhenChargingOverdrive(ShipThrusterController __instance, ref Vector3 __result)
    {
        if ((bool)enableThrustModulator.GetProperty() && (SELocator.GetShipOverdriveController()?.Charging ?? false))
        {
            __result = Vector3.zero;
            return false;
        }

        if ((bool)enableShipTemperature.GetProperty()
            && (float)temperatureDifficulty.GetProperty() > 0f
            && SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio() < -0.5f)
        {
            __result = PatchHandler.AddIgnitionSputter(__instance);
            return false;
        }

        if ((bool)enableEnhancedAutopilot.GetProperty())
        {
            if (SELocator.GetAutopilotPanelController().IsAutopilotActive()
                || SELocator.GetAutopilotPanelController().IsPersistentInputActive())
            {
                __result = Vector3.zero;
                return false;
            }
        }

        return true;
    }
    #endregion

    #region ExtraNoise
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipNoiseMaker), nameof(ShipNoiseMaker.Update))]
    public static bool AddExtraNoises(ShipNoiseMaker __instance)
    {
        if ((float)noiseMultiplier.GetProperty() == 0f)
        {
            __instance._noiseRadius = 0f;
            return false;
        }

        if (!(bool)extraNoise.GetProperty()) return true;

        if (Time.time > __instance._lastImpactTime + 1f)
        {
            __instance._impactNoiseRadius = 0f;
        }

        float thrusterNoiseRadius = Mathf.Lerp(0f, 400f, Mathf.InverseLerp(0f, 20f, __instance._thrusterModel.GetLocalAcceleration().magnitude));
        MasterAlarm masterAlarm = SELocator.GetShipTransform().GetComponentInChildren<MasterAlarm>();
        float alarmNoiseRadius = masterAlarm._isAlarmOn ? 350f : 0f;
        ShipThrusterController thrusterController = SELocator.GetShipTransform().GetComponent<ShipThrusterController>();
        bool shipIgniting = ShipEnhancements.Instance.shipIgniting || PatchHandler.EngineSputtering;
        float ignitionNoiseRadius = shipIgniting ? 500f : 0f;
        float overdriveNoiseRadius = (bool)enableThrustModulator.GetProperty() && SELocator.GetShipOverdriveController() != null
            && SELocator.GetShipOverdriveController().IsCharging() ? 300f : 0f;
        float radioNoiseRadius = 0f;
        if ((bool)addRadio.GetProperty())
        {
            foreach (RadioItem radio in SELocator.GetShipTransform().GetComponentsInChildren<RadioItem>())
            {
                radioNoiseRadius = Mathf.Max(radioNoiseRadius, radio.GetNoiseRadius());
            }
        }

        __instance._noiseRadius = Mathf.Max(thrusterNoiseRadius, __instance._impactNoiseRadius, alarmNoiseRadius, ignitionNoiseRadius, overdriveNoiseRadius,
            radioNoiseRadius);

        if ((float)noiseMultiplier.GetProperty() > 0f)
        {
            __instance._noiseRadius *= (float)noiseMultiplier.GetProperty();
        }

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerNoiseMaker), nameof(PlayerNoiseMaker.Update))]
    public static void AddExtraNoisesToPlayer(PlayerNoiseMaker __instance)
    {
        if (!(bool)extraNoise.GetProperty() || !(bool)addRadio.GetProperty())
        {
            return;
        }

        foreach (RadioItem radio in SELocator.GetPlayerBody().GetComponentsInChildren<RadioItem>())
        {
            __instance._noiseRadius = Mathf.Max(__instance._noiseRadius, radio.GetNoiseRadius());
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipNoiseMaker), nameof(ShipNoiseMaker.Update))]
    public static void ApplyNegativeNoise(ShipNoiseMaker __instance)
    {
        if ((float)noiseMultiplier.GetProperty() < 0f)
        {
            if (__instance._noiseRadius > 0f)
            {
                __instance._noiseRadius = 0f;
            }
            else if (__instance._noiseRadius <= 0f)
            {
                __instance._noiseRadius = 400f;
            }

        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.Explode))]
    public static void AddExplosionNoise(ShipDamageController __instance)
    {
        if ((bool)extraNoise.GetProperty())
        {
            __instance.GetComponentInChildren<ShipNoiseMaker>()._noiseRadius = 1000f * (float)shipExplosionMultiplier.GetProperty();
        }
        if (__instance._explosion != null)
        {
            __instance._explosion.GetComponentInChildren<ExplosionDamage>()?.OnExplode();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HatchController), nameof(HatchController.CloseHatch))]
    public static void ExtraNoiseOnCloseHatch()
    {
        if ((bool)extraNoise.GetProperty())
        {
            SELocator.GetShipTransform().GetComponentInChildren<ShipNoiseMaker>()._noiseRadius += 100f;
        }
    }


    #endregion

    #region NoDamageIndicators
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitUI), nameof(ShipCockpitUI.OnShipHullDamaged))]
    public static bool DisableCockpitUIHullDamaged()
    {
        return !(bool)disableDamageIndicators.GetProperty();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitUI), nameof(ShipCockpitUI.OnShipHullRepaired))]
    public static bool DisableCockpitUIHullRepaired()
    {
        ShipRepairLimitController.AddPartRepaired();
        return !(bool)disableDamageIndicators.GetProperty();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitUI), nameof(ShipCockpitUI.OnShipComponentDamaged))]
    public static bool DisableCockpitUIComponentDamaged(ShipComponent shipComponent)
    {
        return !(bool)disableDamageIndicators.GetProperty();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitUI), nameof(ShipCockpitUI.OnShipComponentRepaired))]
    public static bool DisableCockpitUIComponentRepaired(ShipComponent shipComponent)
    {
        ShipRepairLimitController.AddPartRepaired();
        return !(bool)disableDamageIndicators.GetProperty();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDamageDisplayV2), nameof(ShipDamageDisplayV2.UpdateDisplay))]
    public static bool DisableDamageDisplay(ShipDamageDisplayV2 __instance)
    {
        if ((bool)disableDamageIndicators.GetProperty())
        {
            return false;
        }

        bool allEnabled = !(bool)disableGravityCrystal.GetProperty() && !(bool)disableHeadlights.GetProperty() && !(bool)disableLandingCamera.GetProperty();

        if (allEnabled)
        {
            return true;
        }

        int num = 0;
        if (!__instance._shipDestroyed)
        {
            for (int i = 0; i < 8; i++)
            {
                if (__instance._shipHulls[i] != null && __instance._shipHulls[i].isDamaged)
                {
                    num |= 1 << i;
                }
            }
            for (int j = 0; j < 16; j++)
            {
                if (__instance._shipComponents[j] != null && __instance._shipComponents[j].isDamaged
                    && !(__instance._shipComponents[j] is ShipGravityComponent && (bool)disableGravityCrystal.GetProperty())
                    && !(__instance._shipComponents[j] is ShipHeadlightComponent && (bool)disableHeadlights.GetProperty())
                    && !(__instance._shipComponents[j] is ShipCameraComponent && (bool)disableLandingCamera.GetProperty()))
                {
                    num |= 1 << j + 8;
                }
            }
        }
        __instance._meshRenderer.material.SetInt(__instance._propID_RegionMask, num);

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MasterAlarm), nameof(MasterAlarm.UpdateAlarmState))]
    public static bool DisableAlarm(MasterAlarm __instance)
    {
        if ((bool)disableDamageIndicators.GetProperty())
        {
            return false;
        }
        else if (!ShipEnhancements.Instance.engineOn && (bool)addEngineSwitch.GetProperty())
        {
            return false;
        }
        else if ((bool)preventSystemFailure.GetProperty())
        {
            if (__instance._shipDestroyed)
            {
                return false;
            }
            bool engineAttached = SELocator.GetShipTransform().Find("Module_Engine") != null;
            bool useReactor = __instance._reactorCritical && engineAttached;
            if ((__instance._hullCritical || useReactor) && !__instance._isAlarmOn)
            {
                __instance.TurnOnAlarm();
                return false;
            }
            if (!__instance._hullCritical && !useReactor && __instance._isAlarmOn)
            {
                __instance.TurnOffAlarm();
            }

            return false;
        }

        return true;
    }
    #endregion

    #region ShipSignal
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SignalscopeReticleController), nameof(SignalscopeReticleController.UpdateText))]
    public static bool InsertShipSignalText(SignalscopeReticleController __instance)
    {
        if (!(bool)addShipSignal.GetProperty())
        {
            return true;
        }

        string text = string.Empty;
        if (!PlayerState.AtFlightConsole())
        {
            AudioSignal strongestSignal = __instance._sigScopeTool.GetStrongestSignal();

            if ((strongestSignal != null && strongestSignal.GetName() != ShipEnhancements.Instance.ShipSignalName)
                || (SELocator.GetShipDamageController()?.IsSystemFailed() ?? false))
            {
                return true;
            }

            if (strongestSignal != null && strongestSignal.GetSignalStrength() > SignalscopeUI.s_distanceTextThreshold)
            {
                float distanceFromScope = strongestSignal.GetDistanceFromScope();
                string text2 = "m";
                string text3 = ((distanceFromScope > 1000f) ? __instance._longDistanceHexColor : "ffffffff");
                string text4;
                if (strongestSignal.GetName() == ShipEnhancements.Instance.ShipSignalName)
                {
                    text4 = "SHIP";
                }
                else
                {
                    text4 = (PlayerData.KnowsSignal(strongestSignal.GetName())
                    ? AudioSignal.SignalNameToString(strongestSignal.GetName()) : UITextLibrary.GetString(UITextType.UnknownSignal));
                }

                text = string.Concat(new object[]
                {
                    text4,
                    ": <color=#",
                    text3,
                    ">",
                    Mathf.Round(distanceFromScope),
                    text2,
                    "</color>"
                });
            }
        }
        __instance._scopeDistanceText.text = text;

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.UpdateSignalStrength))]
    public static bool RemoveSignalFromShip(AudioSignal __instance, Signalscope scope, float distToClosestScopeObstruction)
    {
        if (OWInput.IsInputMode(InputMode.ShipCockpit) && SELocator.GetSignalscopeComponent() != null
            && SELocator.GetSignalscopeComponent().isDamaged)
        {
            __instance._canBePickedUpByScope = false;
            __instance._signalStrength = 0f;
            __instance._degreesFromScope = 180f;
            return false;
        }

        if (__instance is ShipAudioSignal)
        {
            (__instance as ShipAudioSignal).UpdateShipSignalStrength(scope, distToClosestScopeObstruction);
            return false;
        }

        return true;
    }
    #endregion

    #region CockpitComponents
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.Awake))]
    public static void AddComponents(ShipDamageController __instance)
    {
        ShipHull cockpit = null;
        foreach (var hull in __instance.GetComponentsInChildren<ShipHull>())
        {
            if (hull.hullName == UITextType.ShipPartForward)
            {
                cockpit = hull;
                break;
            }
        }
        if (cockpit == null) return;

        if ((bool)enableScoutLauncherComponent.GetProperty())
        {
            GameObject probeLauncherComponent = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ProbeLauncherComponent.prefab");
            GameObject componentObj = ShipEnhancements.CreateObject(probeLauncherComponent, cockpit._componentsGroup.transform);
            var comp = componentObj.GetComponent<ProbeLauncherComponent>();
            SELocator.SetProbeLauncherComponent(comp);

            ShipDamageDisplayV2 damageDisplay = cockpit.GetComponentInChildren<ShipDamageDisplayV2>();
            damageDisplay._shipComponents[7] = comp;
            comp.OnDamaged += damageDisplay.OnComponentUpdate;
            comp.OnRepaired += damageDisplay.OnComponentUpdate;
        }
        if ((bool)enableSignalscopeComponent.GetProperty())
        {
            GameObject signalscopeComponent = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/SignalscopeComponent.prefab");
            GameObject componentObj2 = ShipEnhancements.CreateObject(signalscopeComponent, cockpit._componentsGroup.transform);
            SignalscopeComponent comp = componentObj2.GetComponent<SignalscopeComponent>();
            SELocator.SetSignalscopeComponent(comp);

            ShipDamageDisplayV2 damageDisplay = cockpit.GetComponentInChildren<ShipDamageDisplayV2>();
            damageDisplay._shipComponents[8] = comp;
            comp.OnDamaged += damageDisplay.OnComponentUpdate;
            comp.OnRepaired += damageDisplay.OnComponentUpdate;
        }
        if ((string)shipWarpCoreType.GetProperty() == "Component")
        {
            // setting wrong here??
            GameObject warpCoreComponent = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ShipWarpCoreComponent.prefab");
            warpCoreComponent.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = UnityEngine.Object.FindObjectOfType<ShipBody>().gameObject;
            GameObject componentObj = ShipEnhancements.CreateObject(warpCoreComponent, cockpit._componentsGroup.transform);
            var comp = componentObj.GetComponent<ShipWarpCoreComponent>();
            SELocator.SetShipWarpCoreComponent(comp);

            ShipDamageDisplayV2 damageDisplay = cockpit.GetComponentInChildren<ShipDamageDisplayV2>();
            damageDisplay._shipComponents[6] = comp;
            comp.OnDamaged += damageDisplay.OnComponentUpdate;
            comp.OnRepaired += damageDisplay.OnComponentUpdate;

            if (ShipEnhancements.NHAPI == null && GameObject.Find("TimberHearth_Body"))
            {
                GameObject receiver = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ShipWarpReceiver.prefab");
                receiver.GetComponentInChildren<SingularityWarpEffect>()._warpedObjectGeometry = UnityEngine.Object.FindObjectOfType<ShipBody>().gameObject;
                GameObject receiverObj = ShipEnhancements.CreateObject(receiver, GameObject.Find("TimberHearth_Body").transform);
                componentObj.GetComponentInChildren<ShipWarpCoreController>().SetReceiver(receiverObj.GetComponent<ShipWarpCoreReceiver>());
            }
            else
            {
                ShipEnhancements.Instance.WaitForCustomSpawnLoaded();
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipHull), nameof(ShipHull.Awake))]
    public static void AddSignalscopeComponentToList(ShipHull __instance)
    {
        if ((bool)enableSignalscopeComponent.GetProperty() && __instance.hullName == UITextType.ShipPartForward)
        {
            List<ShipComponent> comps = [.. __instance._components];
            comps.Add(SELocator.GetSignalscopeComponent());
            __instance._components = [.. comps];
        }
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(UITextLibrary), nameof(UITextLibrary.GetString))]
    public static string UITextLibrary_GetString(UITextType TextID)
    {
        return "";
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UITextLibrary), nameof(UITextLibrary.GetString))]
    public static void InjectComponentNames(UITextType TextID, ref string __result)
    {
        if (ShipEnhancements.Instance.shipLoaded
            && (bool)enableGasLeak.GetProperty() && PatchHandler.SwapRepairPrompt
            && TextID.ToString().Contains("ShipPart"))
        {
            __result = UITextLibrary_GetString(PatchHandler.FakeRepairPrompt);
            return;
        }

        if (TextID == ShipEnhancements.Instance.ProbeLauncherName)
        {
            __result = "SCOUT LAUNCHER";
        }
        else if (TextID == ShipEnhancements.Instance.SignalscopeName)
        {
            __result = "SIGNALSCOPE";
        }
        else if (TextID == ShipEnhancements.Instance.WarpCoreName)
        {
            __result = "WARP CORE";
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitUI), nameof(ShipCockpitUI.FixedUpdate))]
    public static bool DisableSignalscopeAnimation(ShipCockpitUI __instance)
    {
        if (!(bool)enableSignalscopeComponent.GetProperty()) return true;

        __instance.UpdateShipConsole();
        __instance.UpdateSignalscopeCanvas();
        if (__instance._displayProbeLauncherScreen && __instance._probeScreenT < 1f)
        {
            __instance._probeScreenT = Mathf.Clamp01(__instance._probeScreenT + Time.deltaTime / __instance._probeRotateTime);
            __instance._probeLauncherDisplay.rotation = Quaternion.Lerp(__instance._probeLauncherStowRotation.rotation, __instance._probeLauncherDisplayRotation.rotation, Mathf.SmoothStep(0f, 1f, __instance._probeScreenT));
            if (__instance._probeScreenT >= 1f)
            {
                __instance._shipAudioController.StopProbeScreenMotor();
            }
        }
        else if (!__instance._displayProbeLauncherScreen && __instance._probeScreenT > 0f)
        {
            __instance._probeScreenT = Mathf.Clamp01(__instance._probeScreenT - Time.deltaTime / __instance._probeRotateTime);
            __instance._probeLauncherDisplay.rotation = Quaternion.Lerp(__instance._probeLauncherStowRotation.rotation, __instance._probeLauncherDisplayRotation.rotation, Mathf.SmoothStep(0f, 1f, __instance._probeScreenT));
            if (__instance._probeScreenT <= 0f)
            {
                __instance._shipAudioController.StopProbeScreenMotor();
            }
        }
        if (__instance._displaySignalscopeScreen && __instance._signalscopeScreenT < 1f)
        {
            __instance._signalscopeScreenT = Mathf.Clamp01(__instance._signalscopeScreenT + Time.deltaTime / __instance._scopeRotateTime);
            __instance._sigScopeDisplay.rotation = Quaternion.Lerp(__instance._sigScopeStowRotation.rotation, __instance._sigScopeDisplayRotation.rotation, Mathf.SmoothStep(0f, 1f, __instance._signalscopeScreenT));
            if (!SELocator.GetSignalscopeComponent().isDamaged)
            {
                __instance._sigScopeDish.localEulerAngles = new Vector3(Mathf.SmoothStep(0f, 90f, __instance._signalscopeScreenT), 0f, 0f);
            }
            if (__instance._signalscopeScreenT >= 1f)
            {
                __instance._shipAudioController.StopSigScopeSlide();
                __instance._shipAudioController.PlaySignalscopeInPosition();
                return false;
            }
        }
        else if (!__instance._displaySignalscopeScreen && __instance._signalscopeScreenT > 0f)
        {
            __instance._signalscopeScreenT = Mathf.Clamp01(__instance._signalscopeScreenT - Time.deltaTime / __instance._scopeRotateTime);
            __instance._sigScopeDisplay.rotation = Quaternion.Lerp(__instance._sigScopeStowRotation.rotation, __instance._sigScopeDisplayRotation.rotation, Mathf.SmoothStep(0f, 1f, __instance._signalscopeScreenT));
            if (!SELocator.GetSignalscopeComponent().isDamaged)
            {
                __instance._sigScopeDish.localEulerAngles = new Vector3(Mathf.SmoothStep(0f, 90f, __instance._signalscopeScreenT), 0f, 0f);
            }
            if (__instance._signalscopeScreenT <= 0f)
            {
                __instance._shipAudioController.StopSigScopeSlide();
                __instance._shipAudioController.PlaySignalscopeInPosition();
            }
        }

        return false;
    }
    #endregion

    #region NoSeatbelt
    private static Vector3 _lastImpactVelocity;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.OnImpact))]
    public static void UnbucklePlayerOnImpact(ShipDamageController __instance, ImpactData impact)
    {
        if ((bool)disableSeatbelt.GetProperty() && PlayerState.AtFlightConsole() && impact.speed > 25f)
        {
            _lastImpactVelocity = impact.velocity.normalized * -impact.speed / 40f;
            ShipCockpitController cockpit = SELocator.GetShipTransform().GetComponentInChildren<ShipCockpitController>();
            if (cockpit != null)
            {
                cockpit.ExitFlightConsole();
                cockpit._exitFlightConsoleTime -= 0.2f;
            }

            if (ShipEnhancements.InMultiplayer)
            {
                foreach (uint id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendDetachAllPlayers(id, _lastImpactVelocity);
                }
            }
        }
        if (ShipEnhancements.AchievementsAPI != null)
        {
            if ((float)shipInputLatency.GetProperty() >= 3f && !SEAchievementTracker.BadInternet
                && impact.otherBody.IsKinematic() && impact.otherBody != SELocator.GetShipBody().GetOrigParentBody())
            {
                SEAchievementTracker.BadInternet = true;
                ShipEnhancements.AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.BAD_INTERNET");
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FluidDetector), nameof(FluidDetector.OnEnterFluidType_Internal))]
    public static void UnbucklePlayerOnFluidImpact(FluidDetector __instance, FluidVolume fluid)
    {
        if ((bool)disableSeatbelt.GetProperty() && PlayerState.AtFlightConsole() && __instance is ShipFluidDetector)
        {
            Vector3 vector = fluid.GetPointFluidVelocity(__instance.transform.position, __instance) - __instance._owRigidbody.GetVelocity();
            float pointDensity = fluid.GetPointDensity(__instance.transform.position, __instance);
            float fractionSubmerged = fluid.GetFractionSubmerged(__instance);
            Vector3 impactVelocity = __instance.CalculateDragVelocityChange(vector, pointDensity, fractionSubmerged);
            if (impactVelocity.magnitude > 10f)
            {
                _lastImpactVelocity = -impactVelocity / 25f;
                ShipCockpitController cockpit = SELocator.GetShipTransform().GetComponentInChildren<ShipCockpitController>();
                if (cockpit != null)
                {
                    cockpit.ExitFlightConsole();
                    cockpit._exitFlightConsoleTime -= 0.2f;
                }

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendDetachAllPlayers(id, _lastImpactVelocity);
                    }
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.CompleteExitFlightConsole))]
    public static void ApplyCrashForceToPlayer()
    {
        if ((bool)disableSeatbelt.GetProperty() && _lastImpactVelocity != Vector3.zero)
        {
            SELocator.GetPlayerBody().AddForce(_lastImpactVelocity);
            _lastImpactVelocity = Vector3.zero;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipAudioController), nameof(ShipAudioController.PlayBuckle))]
    public static bool DisableBuckleAudio()
    {
        return !(bool)disableSeatbelt.GetProperty();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipAudioController), nameof(ShipAudioController.PlayUnbuckle))]
    public static bool DisableUnbuckleAudio()
    {
        return !(bool)disableSeatbelt.GetProperty();
    }
    #endregion

    #region Achievements
    [HarmonyPrefix]
    [HarmonyPriority(410)]
    [HarmonyPatch(typeof(ShipHull), nameof(ShipHull.FixedUpdate))]
    public static void HulkSmashAchievementPre(ShipHull __instance, out bool __state)
    {
        __state = false;

        if (ShipEnhancements.AchievementsAPI == null
            || SEAchievementTracker.HulkSmash) return;

        if (__instance._dominantImpact != null
            && __instance._dominantImpact.otherBody is PlayerBody)
        {
            __state = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipHull), nameof(ShipHull.FixedUpdate))]
    public static void HulkSmashAchievementPost(ShipHull __instance, bool __state)
    {
        if (__state && __instance.integrity <= 0f)
        {
            ShipEnhancements.Instance.ModHelper.Events.Unity.FireInNUpdates(() =>
            {
                if (SELocator.GetShipDamageController().IsSystemFailed()
                    && !SEAchievementTracker.HulkSmash)
                {
                    SEAchievementTracker.HulkSmash = true;
                    ShipEnhancements.AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.HULK_SMASH");
                }
            }, 5);
        }
    }
    #endregion

    #region DisableAutoLights
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipElectricalComponent), nameof(ShipElectricalComponent.Start))]
    public static bool KeepPowerOnWhenStart(ShipElectricalComponent __instance)
    {
        if ((bool)disableAutoLights.GetProperty())
        {
            __instance._electricalSystem.SetPowered(!__instance.isDamaged && !(bool)addEngineSwitch.GetProperty());
            return false;
        }

        return true;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ShipComponent), nameof(ShipComponent.OnExitShip))]
    public static void ShipComponent_OnExitShip(ShipComponent __instance) { }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipElectricalComponent), nameof(ShipElectricalComponent.OnExitShip))]
    public static bool KeepPowerOnWhenExit(ShipElectricalComponent __instance)
    {
        if ((bool)disableAutoLights.GetProperty())
        {
            ShipComponent_OnExitShip(__instance);
            return false;
        }

        return true;
    }
    #endregion

    #region ChaoticCyclones
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TornadoFluidVolume), nameof(TornadoFluidVolume.GetPointFluidAngularVelocity))]
    public static void ChaoticTornadoRotation(TornadoFluidVolume __instance, FluidDetector detector, ref Vector3 __result)
    {
        if ((float)cycloneChaos.GetProperty() > 0f && detector.CompareTag("ShipDetector"))
        {
            float upMult = 0.4f * Mathf.Sin(1.5f * Time.time) + 0.3f * Mathf.Cos(5f * Time.time) + 1f;
            Vector3 upRotation = __instance._tornadoPivot.transform.right * upMult;
            float speedMult = 0.5f * Mathf.Cos(2f * Time.time) + 1.2f;
            Vector3 chaos = (__instance._tornadoPivot.transform.up - upRotation)
                * __instance._angularSpeed * speedMult;
            __result = Vector3.Lerp(__result, chaos, (float)cycloneChaos.GetProperty());
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TornadoFluidVolume), nameof(TornadoFluidVolume.GetPointFluidVelocity))]
    public static void ChaoticTornadoSpeed(TornadoFluidVolume __instance, Vector3 worldPosition, FluidDetector detector, ref Vector3 __result)
    {
        if ((float)cycloneChaos.GetProperty() >= 0.5f && detector.CompareTag("ShipDetector"))
        {
            float speedMult = 0.6f * Mathf.Sin(3f * Time.time) + 1f;

            Vector3 pointVelocity = __instance._attachedBody.GetPointVelocity(worldPosition);
            Vector3 vector = Vector3.ProjectOnPlane(__instance._tornadoPivot.position - worldPosition, __instance._tornadoPivot.up);
            float magnitude = vector.magnitude;
            float num = Mathf.InverseLerp(20f, 0f, magnitude);
            float num2 = Mathf.Lerp(__instance._inwardSpeed * 3f, 0f, num);
            Vector3 chaos = pointVelocity + __instance._tornadoPivot.up * __instance._verticalSpeed * speedMult + vector.normalized * num2;

            float lerp = Mathf.InverseLerp(0.5f, 1f, (float)cycloneChaos.GetProperty());
            __result = Vector3.Lerp(__result, chaos, lerp);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PriorityDetector), nameof(PriorityDetector.AddVolume))]
    public static void ForceTornadoPriority(PriorityDetector __instance, EffectVolume eVol, ref object __state)
    {
        if (__instance is not ShipFluidDetector || eVol is not TornadoFluidVolume) return;

        PriorityVolume vol = eVol as PriorityVolume;

        __state = (vol, vol.GetPriority());
        vol.SetPriority(4);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PriorityDetector), nameof(PriorityDetector.AddVolume))]
    public static void ForceTornadoPriority(PriorityDetector __instance, EffectVolume eVol, object __state)
    {
        if (__state == null || __instance is not ShipFluidDetector || eVol is not TornadoFluidVolume) return;
        (PriorityVolume, int) dos = ((PriorityVolume, int))__state;
        dos.Item1.SetPriority(dos.Item2);
    }
    #endregion

    #region ToolInteractFix
    public static void UpdateFocusedItems(bool focused)
    {
        PatchHandler.UpdateFocusedItems(focused);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
    public static bool PreventToolChange(ToolMode mode)
    {
        bool sameBinding = false;
        switch (mode)
        {
            case ToolMode.SignalScope:
                sameBinding = InputLibrary.interactSecondary.HasSameBinding(InputLibrary.signalscope, OWInput.UsingGamepad());
                break;
            case ToolMode.Probe:
                sameBinding = InputLibrary.interactSecondary.HasSameBinding(InputLibrary.probeLaunch, OWInput.UsingGamepad()
                    || InputLibrary.interactSecondary.HasSameBinding(InputLibrary.probeRetrieve, OWInput.UsingGamepad()));
                break;
        }

        return !sameBinding || mode == ToolMode.None || !PatchHandler.HasFocusedItem();
    }
    #endregion

    #region FuelTankExplosion
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ImpactSensor), nameof(ImpactSensor.OnCollisionEnter))]
    public static void ImpactFuelTank(ImpactSensor __instance, Collision collision)
    {
        if (collision.rigidbody == null) return;

        FuelTankItem item;
        Transform parent = null;
        if (__instance.GetComponentInChildren<ForceDetector>()?.CompareTag("DynamicPropDetector") ?? false)
        {
            item = __instance.GetComponentInChildren<FuelTankItem>();
            parent = collision.rigidbody.transform;
        }
        else
        {
            item = collision.collider.GetComponentInParent<FuelTankItem>();
        }

        if (item != null)
        {
            OWRigidbody requiredComponent = collision.rigidbody.GetRequiredComponent<OWRigidbody>();
            ImpactData impactData = new ImpactData(__instance._owRigidbody, requiredComponent, collision);
            item.OnImpact(impactData, parent);
        }
    }
    #endregion

    #region SingleUseTractorBeam
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipTractorBeamSwitch), nameof(ShipTractorBeamSwitch.DeactivateTractorBeam))]
    public static void DisableTractorBeamModel(ShipTractorBeamSwitch __instance)
    {
        if ((bool)singleUseTractorBeam.GetProperty())
        {
            GameObject model = SELocator.GetShipTransform()
                .Find("Module_Cabin/Geo_Cabin/Cabin_Tech/Cabin_Tech_Interior/Props_NOM_SmallTractorBeam_Glow").gameObject;
            if (model != null && model.activeInHierarchy)
            {
                model.SetActive(false);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipTractorBeamSwitch), nameof(ShipTractorBeamSwitch.ActivateTractorBeam))]
    public static bool PreventTractorBeamActivate()
    {
        if ((bool)singleUseTractorBeam.GetProperty())
        {
            return false;
        }

        return true;
    }
    #endregion

    #region DisableRetroRockets
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ThrusterModel), nameof(ThrusterModel.AddTranslationalInput))]
    public static void DisableRetroRockets(ThrusterModel __instance, ref Vector3 input)
    {
        string disableOption = (string)disableThrusters.GetProperty();
        if (disableOption == "None" || __instance is not ShipThrusterModel) return;

        switch (disableOption)
        {
            case "Backward":
                input = new Vector3(input.x, input.y, Mathf.Max(input.z, 0f));
                break;
            case "Left-Right":
                input = new Vector3(0f, input.y, input.z);
                break;
            case "Up-Down":
                input = new Vector3(input.x, 0f, input.z);
                break;
            case "All Except Forward":
                input = new Vector3(0f, 0f, Mathf.Max(input.z, 0f));
                break;
        }
    }
    #endregion

    #region RepairTimeMultiplier
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipComponent), nameof(ShipComponent.RepairTick))]
    public static bool SpecialComponentRepairs(ShipComponent __instance)
    {
        if ((float)repairTimeMultiplier.GetProperty() > 0f)
        {
            return ApplyComponentWrenchMultiplier(__instance);
        }

        if (!__instance._damaged)
        {
            return false;
        }

        if ((float)repairTimeMultiplier.GetProperty() == 0)
        {
            __instance._repairFraction = 1f;
            __instance.SetDamaged(false);
            if (__instance._damageEffect)
            {
                __instance._damageEffect.SetEffectBlend(0f);
            }
            return false;
        }
        else if ((float)repairTimeMultiplier.GetProperty() < 0f && __instance is ShipReactorComponent)
        {
            if (__instance._repairFraction <= 0f)
            {
                ErnestoDetectiveController.ItWasExplosion(sabotage: true);
                SELocator.GetShipDamageController().Explode();
            }
            return false;
        }

        return ApplyComponentWrenchMultiplier(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipHull), nameof(ShipHull.RepairTick))]
    public static bool SpecialHullRepairs(ShipHull __instance)
    {
        if ((float)repairTimeMultiplier.GetProperty() > 0f)
        {
            return ApplyHullWrenchMultiplier(__instance);
        }

        if (!__instance._damaged)
        {
            return false;
        }

        if ((float)repairTimeMultiplier.GetProperty() == 0)
        {
            __instance._integrity = 1f;
            __instance._damaged = false;

            var repairDelegate = (MulticastDelegate)typeof(ShipHull).GetField("OnRepaired", BindingFlags.Instance
                | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__instance);
            if (repairDelegate != null)
            {
                foreach (var handler in repairDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, [__instance]);
                }
            }

            if (__instance._damageEffect)
            {
                __instance._damageEffect.SetEffectBlend(0f);
            }
            return false;
        }
        else if ((float)repairTimeMultiplier.GetProperty() < 0f)
        {
            __instance._integrity = Mathf.Clamp01(__instance._integrity + Time.deltaTime / __instance._repairTime);
            if (__instance._integrity >= 1f)
            {
                __instance._damaged = false;

                var repairDelegate = (MulticastDelegate)typeof(ShipHull).GetField("OnRepaired", BindingFlags.Instance
                | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__instance);
                if (repairDelegate != null)
                {
                    foreach (var handler in repairDelegate.GetInvocationList())
                    {
                        handler.Method.Invoke(handler.Target, [__instance]);
                    }
                }
            }
            if (__instance._damageEffect != null)
            {
                if (__instance._damageEffect is CockpitDamageEffect)
                {
                    CockpitDamageEffect effect = __instance._damageEffect as CockpitDamageEffect;

                    float blend2 = effect._blend;
                    HullDamageEffect_SetEffectBlend(effect, 1f - __instance._integrity);

                    /*if (effect._shipAudioController != null && effect._blend > blend2)
                    {
                        effect._shipAudioController.PlayGlassCrackClip();
                    }*/

                    if (effect._cracksRenderer)
                    {
                        effect._matPropBlock_Cracks.SetFloat(effect._propID_Cutoff, 1f - effect._blend);
                        effect._cracksRenderer.SetPropertyBlock(effect._matPropBlock_Cracks);
                    }
                }
                else
                {
                    __instance._damageEffect.SetEffectBlend(1f - __instance._integrity);
                }
            }

            if (__instance._integrity <= 0f)
            {
                if (__instance.shipModule is ShipDetachableModule)
                {
                    (__instance.shipModule as ShipDetachableModule).Detach();
                    if (!(bool)preventSystemFailure.GetProperty()
                        || __instance.shipModule._hulls[0].section != ShipHull.Section.Front)
                    {
                        ErnestoDetectiveController.ItWasHullBreach(sabotage: true);
                    }
                }
                else if (__instance.shipModule is ShipLandingModule)
                {
                    (__instance.shipModule as ShipLandingModule).DetachAllLegs();
                }
            }
            return false;
        }

        return ApplyHullWrenchMultiplier(__instance);
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(HullDamageEffect), nameof(HullDamageEffect.SetEffectBlend))]
    public static void HullDamageEffect_SetEffectBlend(HullDamageEffect instance, float blend) { }

    public static bool ApplyComponentWrenchMultiplier(ShipComponent __instance)
    {
        if ((string)repairWrenchType.GetProperty() != "Enabled"
            || Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItemType()
            != ShipEnhancements.Instance.RepairWrenchType) return true;

        if (!__instance._damaged)
        {
            return false;
        }
        __instance._repairFraction = Mathf.Clamp01(__instance._repairFraction + Time.deltaTime / (__instance._repairTime / 3));
        if (__instance._repairFraction >= 1f)
        {
            __instance.SetDamaged(false);
        }
        if (__instance._damageEffect)
        {
            __instance._damageEffect.SetEffectBlend(1f - __instance._repairFraction);
        }

        return false;
    }

    public static bool ApplyHullWrenchMultiplier(ShipHull __instance)
    {
        if ((string)repairWrenchType.GetProperty() != "Enabled"
            || Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItemType()
            != ShipEnhancements.Instance.RepairWrenchType) return true;

        if (!__instance._damaged)
        {
            return false;
        }

        __instance._integrity = Mathf.Min(__instance._integrity + Time.deltaTime / (__instance._repairTime / 3), 1f);

        if (__instance._integrity >= 1f)
        {
            __instance._damaged = false;
            var repairDelegate = (MulticastDelegate)typeof(ShipHull).GetField("OnRepaired", BindingFlags.Instance
                | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__instance);
            if (repairDelegate != null)
            {
                foreach (var handler in repairDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, [__instance]);
                }
            }
        }

        if (__instance._damageEffect != null)
        {
            __instance._damageEffect.SetEffectBlend(1f - __instance._integrity);
        }

        return false;
    }
    #endregion

    #region AirDragMultiplier
    [HarmonyPrefix]
    [HarmonyPatch(typeof(FluidDetector), nameof(FluidDetector.AddLinearDrag))]
    public static bool AirDragMultiplier(FluidDetector __instance, FluidVolume fluidVolume, float fluidDensity, float fractionSubmerged)
    {
        List<FluidVolume.Type> acceptableTypes = [FluidVolume.Type.AIR, FluidVolume.Type.FOG];
        if (__instance is ShipFluidDetector && fluidVolume is SimpleFluidVolume && acceptableTypes.Contains(fluidVolume.GetFluidType()))
        {
            Vector3 vector = fluidVolume.GetPointFluidVelocity(__instance.transform.position, __instance) - __instance._owRigidbody.GetVelocity();
            __instance._netRelativeFluidVelocity += vector;
            FluidTypeData[] fluidDataByType = __instance._fluidDataByType;
            FluidVolume.Type fluidType = fluidVolume.GetFluidType();
            fluidDataByType[(int)fluidType].relativeVelocity = fluidDataByType[(int)fluidType].relativeVelocity + vector;
            FluidTypeData[] fluidDataByType2 = __instance._fluidDataByType;
            FluidVolume.Type fluidType2 = fluidVolume.GetFluidType();
            fluidDataByType2[(int)fluidType2].density = fluidDataByType2[(int)fluidType2].density + fluidDensity;
            if (fluidVolume.IsAlignmentFluid())
            {
                __instance._alignmentFluidVelocity += vector;
            }
            Vector3 vector2 = __instance.CalculateDragVelocityChange(vector, fluidDensity, fractionSubmerged) / Time.fixedDeltaTime;
            __instance._netLinearAcceleration += vector2 * (float)airDragMultiplier.GetProperty();

            return false;
        }

        return true;
    }
    #endregion

    #region StunDamage
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.OnHullImpact))]
    public static void LockUpShipControls(ShipDamageController __instance, ImpactData impact, float damage)
    {
        if (!(bool)enableStunDamage.GetProperty() || SELocator.GetShipDamageController().IsSystemFailed()) return;

        float time;
        
        if (damage > 0.1f)
        {
            float lerp = Mathf.InverseLerp(0.1f, 0.8f, damage);
            time = Mathf.Lerp(1f, 8f, lerp);
        }
        else if (impact.speed > 30f * (float)shipDamageSpeedMultiplier.GetProperty())
        {
            float lerp = Mathf.InverseLerp(30f, 120f, impact.speed);
            time = Mathf.Lerp(2f, 5f, lerp);
        }
        else
        {
            return;
        }
        
        __instance.GetComponentInChildren<ShipCockpitController>().LockUpControls(time);
        ShipNotifications.PostStunDamageNotification(time);
        __instance.GetComponent<ShipThrusterController>().SetRollMode(false, 1);
    }
    #endregion

    #region ShipGravityFix
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDirectionalForceVolume), nameof(ShipDirectionalForceVolume.CalculateForceAccelerationOnBody))]
    public static bool ShipGravityFix(ShipDirectionalForceVolume __instance, OWRigidbody targetBody, ref Vector3 __result)
    {
        Vector3 vector = __instance._attachedBody.GetAcceleration();
        if (vector.magnitude > 20f)
        {
            vector -= vector.normalized * 20f;
        }
        else
        {
            vector = Vector3.zero;
        }
        /*if (!__instance._insideSpeedLimiter)
        {
            targetBody.AddAcceleration(vector);
        }*/
        if (targetBody == __instance._playerBody && PlayerState.IsInsideShip() && !PlayerState.IsAttached())
        {
            Vector3 vector2 = (__instance.transform.TransformDirection(-__instance._fieldDirection.normalized * __instance._fieldMagnitude) - vector) * targetBody.GetMass();
            __instance._attachedBody.AddForce(vector2, targetBody.GetWorldCenterOfMass());
        }
        __result = __instance.CalculateForceAccelerationAtPoint(targetBody.GetWorldCenterOfMass());
        return false;
    }
    #endregion

    #region PriorityScreenPrompt
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ScreenPrompt), nameof(ScreenPrompt.IsVisible))]
    public static void PriorityScreenPrompts(ScreenPrompt __instance, ref bool __result)
    {
        if (__instance is PriorityScreenPrompt)
        {
            __result = __instance._isVisible && GUIMode.AreScreenPromptsVisible();
        }
    }
    #endregion

    #region RemovableGravityCrystal
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipGravityComponent), nameof(ShipGravityComponent.OnEnterShip))]
    public static void KeepGravityAudioOff(ShipGravityComponent __instance)
    {
        if ((bool)enableRemovableGravityCrystal.GetProperty() && !__instance.GetComponentInChildren<Light>().enabled)
        {
            __instance._gravityAudio.FadeOut(0f);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipComponent), nameof(ShipComponent.SetDamaged))]
    public static bool KeepGravityCrystalFixed(ShipComponent __instance, bool damaged)
    {
        if ((bool)enableRemovableGravityCrystal.GetProperty() && damaged && __instance is ShipGravityComponent
            && !__instance.GetComponentInChildren<Light>().enabled)
        {
            return false;
        }

        return true;
    }
    #endregion

    #region HornfelsGroundedYou
    [HarmonyPrefix]
    [HarmonyPatch(typeof(LaunchElevatorController), nameof(LaunchElevatorController.OnPressInteract))]
    public static bool CancelLaunchCodesInput()
    {
        if (ShipEnhancements.Instance.groundedByHornfels)
        {
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LaunchElevatorController), nameof(LaunchElevatorController.OnLearnLaunchCodes))]
    public static bool CancelLearnLaunchCodes(LaunchElevatorController __instance)
    {
        if (ShipEnhancements.Instance.groundedByHornfels)
        {
            __instance._launchElevator._interactVolume.ChangePrompt("Grounded by Hornfels");
            __instance._launchElevator._interactVolume.SetKeyCommandVisible(false);
            return false;
        }
        return true;
    }
    #endregion

    #region DialogueEntryConditionFix
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DialogueNode), nameof(DialogueNode.EntryConditionsSatisfied))]
    public static bool DialogueEntryConditi4onsSatisfied(DialogueNode __instance, ref bool __result)
    {
        if (ModCompatibility.VanillaFix) return false;

        bool flag = true;
        if (__instance._listEntryCondition.Count == 0)
        {
            __result = false;
            return false;
        }
        DialogueConditionManager sharedInstance = DialogueConditionManager.SharedInstance;
        for (int i = 0; i < __instance._listEntryCondition.Count; i++)
        {
            string text = __instance._listEntryCondition[i];
            // CHANGED: remove the !
            if (PlayerData.PersistentConditionExists(text))
            {
                if (!PlayerData.GetPersistentCondition(text))
                {
                    flag = false;
                }
            }
            else if (sharedInstance.ConditionExists(text))
            {
                if (!sharedInstance.GetConditionState(text))
                {
                    flag = false;
                }
            }
            else
            {
                flag = false;
            }
        }
        __result = flag;
        return false;
    }
    #endregion

    #region ShipRepairLimit
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RepairReceiver), nameof(RepairReceiver.IsRepairable))]
    public static void ApplyShipRepairLimit(RepairReceiver __instance, ref bool __result)
    {
        bool isShipType = __instance.type == RepairReceiver.Type.ShipHull || __instance.type == RepairReceiver.Type.ShipComponent;
        if (__result && isShipType)
        {
            __result = ShipRepairLimitController.CanRepair() && ((string)repairWrenchType.GetProperty() != "Required"
                || Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItemType() == ShipEnhancements.Instance.RepairWrenchType);
        }
    }
    #endregion

    #region PreventSystemFailure
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.OnModuleDetach))]
    public static bool CheckDetachedModule(ShipDamageController __instance, ShipDetachableModule module)
    {
        if (!(bool)preventSystemFailure.GetProperty() || module.CompareTag("ShipCockpit"))
        {
            return true;
        }

        if (PlayerState.IsInsideShip() && Locator.GetPlayerSuit().IsWearingSuit(true) && !Locator.GetPlayerSuit().IsWearingHelmet())
        {
            Locator.GetPlayerSuit().PutOnHelmet();
        }

        HullBreachEntrywayTrigger hullBreachTrigger = __instance.GetComponentInChildren<HullBreachEntrywayTrigger>();

        if (!hullBreachTrigger.HasBreached())
        {
            hullBreachTrigger.OnHullBreached();
            GlobalMessenger.FireEvent("ShipHullDetached");
        }

        if (module.GetComponent<ShipHull>().section == ShipHull.Section.Left)
        {
            if (!(bool)enableRemovableGravityCrystal.GetProperty() || GameObject.Find("Module_Engine_Body").GetComponentInChildren<ShipGravityCrystalItem>() != null)
            {
                ShipGravityComponent gravityComponent = GameObject.Find("Module_Engine_Body").GetComponentInChildren<ShipGravityComponent>();
                gravityComponent._repairReceiver.repairDistance = 0f;
                gravityComponent._damaged = true;
                gravityComponent._repairFraction = 0f;
                gravityComponent.OnComponentDamaged();
                gravityComponent._damageEffect.SetEffectBlend(1f - gravityComponent._repairFraction);
            }

            __instance.GetComponentInChildren<ExplosionController>().transform.parent = module.transform;

            __instance.GetComponent<ShipThrusterModel>().SetThrusterBankEnabled(ThrusterBank.Left, false);
            hullBreachTrigger.EnableEngineEntryway();
        }
        else if (module.GetComponent<ShipHull>().section == ShipHull.Section.Right)
        {
            __instance.GetComponent<ShipThrusterModel>().SetThrusterBankEnabled(ThrusterBank.Right, false);
            hullBreachTrigger.EnableSuppliesEntryway();
        }

        SELocator.GetShipResources().OnShipHullBreach();
        __instance.GetComponentInChildren<MasterAlarm>().OnShipDamageUpdated();

        for (int j = 0; j < __instance._stencils.Length; j++)
        {
            __instance._stencils[j].SetActive(false);
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.Explode))]
    public static bool PreventExplosionDamage(ShipDamageController __instance, bool debug)
    {
        if (!(bool)preventSystemFailure.GetProperty()) return true;

        if (!__instance._exploded && !__instance._invincible && !debug && __instance.transform.Find("Module_Engine") == null)
        {
            if (__instance._explosion != null)
            {
                __instance._explosion.Play();
            }

            __instance._exploded = true;
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.GetLowestHullIntegrity))]
    public static bool IgnoreDetachedHulls(ShipDamageController __instance, ref float __result)
    {
        if (!(bool)preventSystemFailure.GetProperty()) return true;

        float num = 1f;
        for (int i = 0; i < __instance._shipHulls.Length; i++)
        {
            if (__instance._shipHulls[i].integrity < num && __instance.GetComponentInParent<ShipBody>())
            {
                num = __instance._shipHulls[i].integrity;
            }
        }
        __result = num;
        return false;
    }
    #endregion

    #region ErnestoComments
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.OnHullImpact))]
    public static void ErnestoHeavyImpact(ShipDamageController __instance, float damage)
    {
        if ((bool)addErnesto.GetProperty() && !__instance._exploded && damage > 0.3f)
        {
            SELocator.GetErnesto().OnHeavyImpact();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ElectricityVolume), nameof(ElectricityVolume.ApplyShock))]
    public static void ErnestoShock(HazardDetector detector)
    {
        if ((bool)addErnesto.GetProperty() && detector.CompareTag("ShipDetector"))
        {
            SELocator.GetErnesto().OnElectricalShock();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipEjectionSystem), nameof(ShipEjectionSystem.Update))]
    public static void ErnestoEjection(ShipEjectionSystem __instance)
    {
        if ((bool)addErnesto.GetProperty() && __instance._ejectPressed)
        {
            SELocator.GetErnesto().OnCockpitDetached();
        }
    }
    #endregion

    #region ErnestoAwareness
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TextTranslation), nameof(TextTranslation.Translate))]
    public static bool TextTranslation_Translate(TextTranslation __instance, string key, ref string __result)
    {
        if (key == "SE_Ernesto_ErnestosERNESTO_PLACEHOLDER")
        {
            int numErnestos = ErnestoModListHandler.GetNumberErnestos();
            string[] lines =
            [
                "I dunno, I can't really feel anything right now.",
                "Everything's all fuzzy right now, sorry.",
                "No idea, but maybe I can tell you later.",
                "Ask me later. I can't feel anything right now."
            ];

            if (numErnestos == 0)
            {
                lines =
                [
                    "No, I'm not sensing any other Ernestos right now. Maybe one will show up later.",
                    "I think I'm the only Ernesto here. Nice to finally get some peace and quiet- oh wait. You're here.",
                    "I'm the only one in this universe. Do you want a different Ernesto or something?",
                    "Nope, it's just me and you.",
                    "No. Guess what that means? There's no one to stop me from hitting you with a rock.",
                    "Narp.",
                ];
                int rand1 = new System.Random().Next(lines.Length);
                __result = lines[rand1];
            }
            else if (numErnestos == 1)
            {
                if (ShipEnhancements.Instance.ModHelper.Interaction.ModExists("xen.NewHorizonsExamples"))
                {
                    lines =
                    [
                        "Yeah, there's only one other Ernesto. I get the feeling I might be related to him.",
                    ];
                }
                else
                {
                    lines =
                    [
                        "Yeah, but there's only one. Maybe you should go say hi to them, they're probably lonely.",
                        "Just me and some other guy I don't know. Not sure why they're here.",
                        "Just one extra Ernesto. Who do you think existed first?",
                        "Yeah, there's another Ernesto here, but honestly this universe would be better off with just one. What? What do you mean it should be him??",
                        "Yarp. Just one, though. I wonder if they have the same name as me?"
                    ];
                }
            }
            else if (numErnestos > 1 && numErnestos <= 8)
            {
                lines =
                [
                    $"There's {numErnestos} extra Ernestos in our universe, which isn't necessarily a bad thing.",
                    $"I think there's {numErnestos} of them, which if my calculations are correct is not a normal amount of Ernestos.",
                    $"Yarp, {numErnestos}. I can't think of a funny quip so that's all you're gonna get.",
                    $"Yeah, {numErnestos} guys. Maybe you can go bother one of them, I'm sure they have some interesting things to say.",
                    $"The anglerfish population suddenly increased by {numErnestos}, if that's what you're asking.",
                ];
            }
            else if (numErnestos > 8 && numErnestos <= 15)
            {
                lines =
                [
                    $"There's {numErnestos} Ernestos. That doesn't sound right.",
                    $"Yeah, there's {numErnestos}, which doesn't feel like a normal amount of Ernestos, but I guess that's what happens when you start mixing worlds.",
                    $"I'm sensing {numErnestos} other Ernestos. That's gotta be at least {numErnestos - 5} who will beat you to death with a rock.",
                    $"Yeah, there's a surplus of magical talking anglerfish right now because {numErnestos} Ernestos decided to cross into our world.",
                ];
            }
            else if (numErnestos > 15 && numErnestos < ErnestoModListHandler.GetMaxErnestos())
            {
                lines =
                [
                    $"Yes, there are a lot. {numErnestos}, to be exact. And that isn't a normal amount of magical talking anglerfish to have in one universe, in case you were wondering.",
                    $"Yeah, {numErnestos} Ernestos. How did you even cram that many into one universe?",
                    $"Look, I don't know what you plan to do with {numErnestos} Ernestos, but whatever it is leave me out of it.",
                    $"Yep. {numErnestos} Ernestos that could potentially beat you to death with a rock. It's a dangerous universe, Hatchling.",
                    $"Yeah, {numErnestos} other magical anglerfish that I could be talking to instead of you.",
                ];
            }
            else if (numErnestos == ErnestoModListHandler.GetMaxErnestos())
            {
                lines =
                [
                    "Oh yeah. Way more Ernestos than there ever should be.",
                    "Look, I don't know what you did Hatchling, but you managed to fit every Ernesto from every world into this universe. It's pretty impressive, honestly.",
                    $"There sure are, and I don't know if this universe can even handle every Ernesto to ever exist. You really made a mistake bringing {numErnestos} of those guys here.",
                    "All of them. Every Ernesto ever.",
                    $"Yarp, {numErnestos} of them. You should go find them all before the fabric of spacetime is destroyed and we all die.",
                    "Yes and there are too many. Please get rid of some of them. The universe is falling apart as we speak.",
                    "There are so many it's practically an invasion. An invasion of magical talking anglerfish."
                ];
            }
            int rand2 = new System.Random().Next(lines.Length);
            __result = lines[rand2];
            return false;
        }
        else if (key == "SE_Ernesto_ClockERNESTO_PLACEHOLDER")
        {
            int randomHour = UnityEngine.Random.Range(1, 13);
            int randomMinute = UnityEngine.Random.Range(10, 60);
            __result = $"It's {randomHour}:{randomMinute} if I'm reading that clock correctly.";
            return false;
        }
        else if (key == "SE_Ernesto_ShipFailureERNESTO_PLACEHOLDER")
        {
            __result = ErnestoDetectiveController.GetHypothesis();
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ElectricityVolume), nameof(ElectricityVolume.ApplyShock))]
    public static void ErnestoDetectElectricityShock(ElectricityVolume __instance, bool __runOriginal, HazardDetector detector)
    {
        if (!(bool)addErnesto.GetProperty() || !__runOriginal) return;

        OWRigidbody attachedOWRigidbody = detector.GetAttachedOWRigidbody(false);
        if (attachedOWRigidbody != null)
        {
            if (detector.CompareTag("ShipDetector"))
            {
                ShipDamageController component = attachedOWRigidbody.GetComponent<ShipDamageController>();
                if (component.IsElectricalFailed())
                {
                    if (!component._shipReactorComponent.isDamaged)
                    {
                        ErnestoDetectiveController.SetReactorCause("electricity");
                    }
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipReactorComponent), nameof(ShipReactorComponent.Update))]
    public static void ErnestoDetectReactorExplosion(ShipReactorComponent __instance)
    {
        if (!(bool)addErnesto.GetProperty()) return;

        if (__instance._damaged && __instance._criticalTimer <= 0f)
        {
            ErnestoDetectiveController.ItWasExplosion(fromReactor: true);
        }
        else if (!__instance._damaged)
        {
            ErnestoDetectiveController.SetReactorCause("");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDetachableModule), nameof(ShipDetachableModule.ApplyImpact))]
    public static void ErnestoDetectHullImpact(ShipDetachableModule __instance, bool __runOriginal)
    {
        if (!(bool)addErnesto.GetProperty() || !__runOriginal
            || SELocator.GetShipDamageController().IsSystemFailed()) return;

        for (int i = 0; i < __instance._hulls.Length; i++)
        {
            if (__instance._hulls[i]._integrity <= 0f && __instance._hulls[i].shipModule is ShipDetachableModule
            && (!(bool)preventSystemFailure.GetProperty() || __instance._hulls[i].section == ShipHull.Section.Front))
            {
                ErnestoDetectiveController.ItWasHullBreach(impact: true);
                return;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.TriggerHullBreach))]
    public static void ErnestoDefaultHullBreach(ShipDamageController __instance)
    {
        if (!(bool)addErnesto.GetProperty()) return;

        ShipEnhancements.Instance.ModHelper.Events.Unity
            .FireInNUpdates(() => ErnestoDetectiveController.ItWasHullBreach(), 3);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.Explode))]
    public static void ErnestoDefaultExplode(ShipDamageController __instance)
    {
        if (!(bool)addErnesto.GetProperty()) return;

        ShipEnhancements.Instance.ModHelper.Events.Unity
            .FireInNUpdates(() => ErnestoDetectiveController.ItWasExplosion(), 3);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipEjectionSystem), nameof(ShipEjectionSystem.Update))]
    public static void ErnestoCockpitEject(ShipEjectionSystem __instance)
    {
        if (!(bool)addErnesto.GetProperty()) return;

        if (__instance._ejectPressed)
        {
            ErnestoDetectiveController.ItWasHullBreach(ejected: true);
        }
    }
    #endregion

    #region FunnySounds
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipAudioController), nameof(ShipAudioController.PlayImpactAtPosition))]
    public static bool ShipBonk(ShipAudioController __instance, float volume, Vector3 worldPos)
    {
        if (!(bool)funnySounds.GetProperty()) return true;

        if (Time.time - __instance._lastImpactTime < 0.5f)
        {
            return false;
        }
        for (int i = 0; i < __instance._hullImpactSources.Length; i++)
        {
            if (!__instance._hullImpactSources[i].isPlaying)
            {
                __instance._lastImpactTime = Time.time;
                __instance._hullImpactSources[i].transform.position = worldPos;
                __instance._hullImpactSources[i].SetLocalVolume(volume);
                __instance._hullImpactSources[i].clip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/bonk.ogg");
                __instance._hullImpactSources[i].Play();
                return false;
            }
        }

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.TriggerSystemFailure))]
    public static void ShipLegoBreak(ShipDamageController __instance)
    {
        if ((bool)funnySounds.GetProperty() && __instance.IsHullBreached())
        {
            ShipAudioController audio = __instance.GetComponentInChildren<ShipAudioController>();
            for (int i = 0; i < audio._hullImpactSources.Length; i++)
            {
                if (!audio._hullImpactSources[i].isPlaying)
                {
                    audio._lastImpactTime = Time.time;
                    audio._hullImpactSources[i].transform.position = SELocator.GetShipTransform().TransformPoint(Vector3.zero);
                    audio._hullImpactSources[i].SetLocalVolume(1f);
                    audio._hullImpactSources[i].clip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/lego_break.ogg");
                    audio._hullImpactSources[i].Play();
                    return;
                }
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipThrusterAudio), nameof(ShipThrusterAudio.OnStartShipIgnition))]
    public static bool ShipCartoonRun(ShipThrusterAudio __instance)
    {
        if (!(bool)funnySounds.GetProperty()) return true;

        __instance._ignitionSource.Stop();
        __instance._isIgnitionPlaying = true;
        __instance._ignitionSource.PlayOneShot(ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/cartoon_run.ogg"), 1f);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipAudioController), nameof(ShipAudioController.PlayRaiseEjectCover))]
    public static bool ShipShotgunEject(ShipAudioController __instance)
    {
        if (!(bool)funnySounds.GetProperty()) return true;

        __instance._ejectCoverSource.PlayOneShot(ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/shotgun.ogg"));
        return false;
    }
    #endregion

    #region DisableShipMedkit
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerRecoveryPoint), nameof(PlayerRecoveryPoint.Awake))]
    public static void DisableShipMedkit(PlayerRecoveryPoint __instance)
    {
        if ((bool)disableShipMedkit.GetProperty() && __instance.GetComponentInParent<ShipBody>())
        {
            __instance._healsPlayer = false;
        }
    }
    #endregion

    #region DisableHazardPrevention
    [HarmonyPostfix]
    [HarmonyPatch(typeof(HazardDetector), nameof(HazardDetector.IsInvulnerable))]
    public static void ShipHazardVulnerability(HazardDetector __instance, ref bool __result)
    {
        if ((bool)disableHazardPrevention.GetProperty() && __result && __instance._isPlayerDetector)
        {
            __result = PlayerState.IsInsideTheEye();
        }
    }
    #endregion

    #region Radio
    [HarmonyPostfix]
    [HarmonyPatch(typeof(RulesetDetector), nameof(RulesetDetector.AllowTravelMusic))]
    public static void DisableTravelMusicForRadio(ref bool __result)
    {
        if (!(bool)addRadio.GetProperty())
        {
            return;
        }

        if (__result)
        {
            foreach (RadioItem radio in SELocator.GetShipTransform().GetComponentsInChildren<RadioItem>())
            {
                if (radio.ShouldOverrideTravelMusic())
                {
                    __result = false;
                    return;
                }
            }
        }
    }
    #endregion

    #region ProlongDigestion
    public static float digestionDamageDelay = 0f;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.Start))]
    public static void SetDigestionLength(AnglerfishController __instance)
    {
        if ((bool)moreExplosionDamage.GetProperty())
        {
            __instance._stunTimer = 100f;
        }
        if ((bool)prolongDigestion.GetProperty())
        {
            __instance._consumeDeathDelay = 8f;
            __instance._consumeShipCrushDelay = 6f;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.OnCaughtObject))]
    public static bool ProlongDigestion(AnglerfishController __instance, OWRigidbody caughtBody)
    {
        if (!(bool)prolongDigestion.GetProperty() || ShipEnhancements.InMultiplayer) return true;

        if (__instance._currentState == AnglerfishController.AnglerState.Consuming)
        {
            if (!__instance._targetBody.CompareTag("Player") && caughtBody.CompareTag("Player")
                && !PlayerState.IsInsideShip() && !PlayerState.AtFlightConsole())
            {
                Locator.GetDeathManager().KillPlayer(DeathType.Digestion);
            }
            return false;
        }
        if (caughtBody.CompareTag("Player") || caughtBody.CompareTag("Ship"))
        {
            __instance._targetBody = caughtBody;
            __instance._consumeStartTime = Time.time;
            __instance.ChangeState(AnglerfishController.AnglerState.Consuming);
            if (caughtBody.CompareTag("Ship"))
            {
                ShipNotifications.PostDigestionNotification();
                digestionDamageDelay = UnityEngine.Random.Range(0.6f, 1.2f);
            }
            if (PlayerState.IsInsideShip() || PlayerState.AtFlightConsole())
            {
                Locator.GetPlayerDeathAudio()._deathSource.PlayOneShot(AudioType.Death_Digestion, 1f);
                if (PlayerState.IsInsideShip())
                {
                    Locator.GetPlayerDeathAudio()._shipGroanSource.PlayDelayed(1f);
                }
            }
        }
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.UpdateState))]
    public static void PermanentStunMode(AnglerfishController __instance)
    {
        if ((bool)moreExplosionDamage.GetProperty() && __instance._currentState == AnglerfishController.AnglerState.Stunned)
        {
            __instance._stunTimer = 100f;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AnglerfishController), nameof(AnglerfishController.UpdateState))]
    public static void ShipDigestionEffects(AnglerfishController __instance)
    {
        if (!(bool)prolongDigestion.GetProperty()) return;

        if (__instance._currentState == AnglerfishController.AnglerState.Consuming)
        {
            if (!__instance._consumeComplete)
            {
                if (__instance._targetBody == null)
                {
                    return;
                }
                float num = Time.time - __instance._consumeStartTime;
                if (__instance._targetBody.CompareTag("Ship"))
                {
                    if (SELocator.GetShipDamageController().IsSystemFailed()) return;

                    if (num > __instance._consumeShipCrushDelay)
                    {
                        SELocator.GetShipTransform().GetComponentInChildren<ShipAudioController>()
                            ._shipElectrics._audioSource.PlayOneShot(AudioType.ShipDamageElectricalFailure, 0.5f);
                        ErnestoDetectiveController.ItWasAnglerfish();
                    }
                    else
                    {
                        if (digestionDamageDelay < 0f)
                        {
                            PatchHandler.RandomShipDamage(damageCause: "anglerfish");
                            digestionDamageDelay = UnityEngine.Random.Range(0.2f, 0.8f);
                        }
                        else
                        {
                            digestionDamageDelay -= Time.deltaTime;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AnglerfishAnimController), nameof(AnglerfishAnimController.OnAnglerUnsuspended))]
    public static bool PreventAnglerAnimWhenStunned(AnglerfishController.AnglerState state)
    {
        return state != AnglerfishController.AnglerState.Stunned;
    }
    #endregion

    #region FluidDamage
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SandFunnelTriggerVolume), nameof(SandFunnelTriggerVolume.IsObjectExposed))]
    public static void ForceModuleDetectorsExposed(SandFunnelTriggerVolume __instance, GameObject obj, ref bool __result)
    {
        if (!obj.GetComponent<StaticFluidDetector>() || !obj.GetComponentInParent<ShipModule>()) return;

        if (!__result)
        {
            __result = __instance.IsObjectExposed(SELocator.GetShipDetector());
        }
    }
    #endregion

    #region NoiseDetectionFix
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NoiseMaker), nameof(NoiseMaker.GetNoiseOrigin))]
    public static void FixShipNoiseOrigin(NoiseMaker __instance, ref Vector3 __result)
    {
        if (!ModCompatibility.VanillaFix && __instance is ShipNoiseMaker)
        {
            __result = __instance._attachedBody.GetWorldCenterOfMass();
        }
    }
    #endregion

    #region DisableMinimapIcons
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetComponentsEnabled))]
    public static bool KeepMinimapOff(Minimap __instance, bool value)
    {
        bool qShip = (bool)enableQuantumShip.GetProperty();
        bool disabled = (bool)disableMinimapMarkers.GetProperty();
        if (!value || (!qShip && !disabled)) return true;

        for (int i = 0; i < __instance._minimapRenderersToSwitchOnOff.Length; i++)
        {
            if (__instance._minimapRenderersToSwitchOnOff[i].transform.parent == __instance._globeMeshTransform)
            {
                __instance._minimapRenderersToSwitchOnOff[i].enabled = value;
            }
            else
            {
                __instance._minimapRenderersToSwitchOnOff[i].enabled = !disabled && (!qShip
                    || __instance._minimapRenderersToSwitchOnOff[i].name != "ShipMarker"
                    || !(Locator.GetQuantumMoon()?._isShipInside ?? false));
            }
        }
        for (int j = 0; j < __instance._electricalComponentsToSwitchOnOff.Length; j++)
        {
            __instance._electricalComponentsToSwitchOnOff[j].SetPowered(value);
        }

        return false;
    }
    #endregion

    #region FlagMarkers
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateMarkers))]
    public static void UpdateFlagMarkers(Minimap __instance, bool __runOriginal)
    {
        if (!__runOriginal || !(bool)addExpeditionFlag.GetProperty()) return;

        if (__instance.TryGetComponent(out MinimapFlagController flagController))
        {
            flagController.UpdateMarkers();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.SetComponentsEnabled))]
    public static void SetFlagMarkersEnabled(Minimap __instance, bool value, bool __runOriginal)
    {
        if (!__runOriginal || !(bool)addExpeditionFlag.GetProperty()) return;

        if (__instance.TryGetComponent(out MinimapFlagController flagController))
        {
            flagController.SetComponentsEnabled(value);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitUI), nameof(ShipCockpitUI.Update))]
    public static void UpdateFlagMarkersOn(ShipCockpitUI __instance)
    {
        if ((bool)addExpeditionFlag.GetProperty() && !(bool)disableMinimapMarkers.GetProperty())
        {
            MinimapFlagController flagController = __instance.GetComponentInChildren<MinimapFlagController>();
            if (flagController == null) return;
            if (__instance._shipSystemsCtrlr.UsingLandingCam() && !__instance._landingCamScreenLight.IsOn())
            {
                flagController.SetComponentsOn(true);
            }
            else if (!__instance._shipSystemsCtrlr.UsingLandingCam() && __instance._landingCamScreenLight.IsOn())
            {
                flagController.SetComponentsOn(false);
            }
        }
    }
    #endregion

    #region ScoutPhotoMode
    // Borrowed from Archipelago Randomizer

    // Many OW players never realized "photo mode" exists, so default to that when they don't have the Scout
    [HarmonyPostfix, HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.Start))]
    public static void ProbeLauncher_Start_Postfix(ProbeLauncher __instance)
    {
        if (!(bool)scoutPhotoMode.GetProperty()) return;

        if ((bool)disableScoutLaunching.GetProperty() && __instance.GetName() == ProbeLauncher.Name.Ship)
        {
            //APRandomizer.OWMLModConsole.WriteLine($"putting the Scout Launcher in photo mode since we don't have the Scout yet");
            __instance._photoMode = true;
        }
    }

    // The above patch also causes "ship photo mode" to be a thing, with inconsistent UI prompts, only until you have Scout.
    // So these two patches allow the ship to properly toggle between photo and launch modes just like you can on foot
    // even after you get Scout, effectively promoting ship photo mode to an intended quality-of-life feature.
    [HarmonyPostfix, HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.AllowPhotoMode))]
    public static void ProbeLauncher_AllowPhotoMode(ProbeLauncher __instance, ref bool __result)
    {
        if (!(bool)scoutPhotoMode.GetProperty()) return;

        if (__instance.GetName() == ProbeLauncher.Name.Ship)
        {
            __result = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.AllowLaunchMode))]
    public static void ProbeLauncher_AllowLaunchMode(ProbeLauncher __instance, ref bool __result)
    {
        if (__instance.GetName() == ProbeLauncher.Name.Ship
            && (bool)disableScoutLaunching.GetProperty() && (bool)scoutPhotoMode.GetProperty())
        {
            __result = false;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.UpdatePreLaunch))]
    public static void ProbeLauncher_UpdatePreLaunch(ProbeLauncher __instance)
    {
        if (!(bool)scoutPhotoMode.GetProperty()) return;

        if ((bool)disableScoutLaunching.GetProperty() && __instance._name == ProbeLauncher.Name.Ship
            && __instance._photoMode) return;

        // copy-pasted from vanilla impl, with the first == changed to !=, so this is effectively:
        // "if the vanilla UpdatePreLaunch is about to ignore a tool left/right press only because
        // this is the ship's scout launcher, then don't ignore it"
        if (__instance._name == ProbeLauncher.Name.Ship && (OWInput.IsNewlyPressed(InputLibrary.toolOptionLeft, InputMode.All)
                || OWInput.IsNewlyPressed(InputLibrary.toolOptionRight, InputMode.All)))
        {
            __instance._photoMode = !__instance._photoMode;
            var ui = __instance._launcherUIs[0];
            if (__instance.InPhotoMode())
            {
                if (ui._bracketImage != null && !ui._bracketImage.enabled)
                {
                    ui._bracketImage.enabled = true;
                }
            }
            else if (ui._bracketImage != null && ui._bracketImage.enabled)
            {
                ui._bracketImage.enabled = false;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncherUI), nameof(ProbeLauncherUI.Update))]
    public static bool FixShipPhotoModeBrackets(ProbeLauncherUI __instance)
    {
        if (!(bool)scoutPhotoMode.GetProperty()
            || __instance._probeLauncher.GetName() != ProbeLauncher.Name.Ship)
        {
            return true;
        }

        if (__instance._probeLauncher.InPhotoMode())
        {
            if (__instance._bracketImage != null && !__instance._bracketImage.enabled)
            {
                __instance._bracketImage.enabled = true;
            }
        }
        else if (__instance._bracketImage != null && __instance._bracketImage.enabled)
        {
            __instance._bracketImage.enabled = false;
        }

        if (__instance._nonSuitUI && ProbeLauncherUI.s_removeSnapshopPrompt.IsVisible() && OWInput.IsNewlyPressed(InputLibrary.cancel, InputMode.All))
        {
            __instance.DeactivateUI();
        }

        return false;
    }
    #endregion

    #region AutopilotOverride
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.Update))]
    public static bool OverrideAutopilotInputs(ShipCockpitController __instance, bool __runOriginal)
    {
        if (!__runOriginal) return false;

        if (!(bool)enableEnhancedAutopilot.GetProperty()) return true;

        if (!__instance._playerAtFlightConsole)
        {
            return false;
        }
        if (__instance._controlsLocked && Time.time >= __instance._controlsUnlockTime)
        {
            __instance._controlsLocked = false;
            __instance._thrustController.enabled = !__instance._shipSystemFailure;
            if (!__instance._shipSystemFailure)
            {
                if (__instance._thrustController.RequiresIgnition() && __instance._landingManager.IsLanded())
                {
                    RumbleManager.SetShipThrottleCold();
                }
                else
                {
                    RumbleManager.SetShipThrottleNormal();
                }
            }
        }
        // cancel if left flight chair
        if (!OWInput.IsInputMode(InputMode.ShipCockpit | InputMode.LandingCam))
        {
            SELocator.GetAutopilotPanelController().CancelMatchVelocity();
            SendAutopilotState(stopMatch: true);
            return false;
        }
        __instance.UpdateShipLightInput();
        // abort autopilot
        if (__instance._autopilot.IsFlyingToDestination() || __instance._autopilot.GetComponent<PidAutopilot>().enabled)
        {
            if (OWInput.IsNewlyPressed(InputLibrary.autopilot, InputMode.All))
            {
                SELocator.GetAutopilotPanelController().CancelAutopilot();
                SendAutopilotState(abort: true);
            }
            if (OWInput.IsNewlyPressed(InputLibrary.matchVelocity, InputMode.All))
            {
                SELocator.GetAutopilotPanelController().CancelMatchVelocity();
                SendAutopilotState(stopMatch: true);
            }
        }
        else
        {
            // start autopilot
            if (__instance._playerAtFlightConsole && !__instance._shipSystemFailure
                && OWInput.IsNewlyPressed(InputLibrary.autopilot, InputMode.ShipCockpit))
            {
                InputLibrary.lockOn.BlockNextRelease();

                SELocator.GetAutopilotPanelController().ActivateAutopilot();
                SendAutopilotState(SELocator.GetReferenceFrame()?.GetOWRigidBody(), destination: true);
            }
            // start velocity match
            if (__instance._playerAtFlightConsole && !__instance._shipSystemFailure
                && OWInput.IsNewlyPressed(InputLibrary.matchVelocity, InputMode.All))
            {
                SELocator.GetAutopilotPanelController().ActivateMatchVelocity();
                SendAutopilotState(SELocator.GetReferenceFrame(ignorePassiveFrame: false)?.GetOWRigidBody(), startMatch: true);
            }
            // stop velocity match
            else if (OWInput.IsNewlyReleased(InputLibrary.matchVelocity, InputMode.All)
                && (ShipEnhancements.GEInteraction == null || !ShipEnhancements.GEInteraction.IsContinuousMatchVelocityEnabled()))
            {
                SELocator.GetAutopilotPanelController().CancelMatchVelocity();
                SendAutopilotState(stopMatch: true);
            }
        }
        if (!__instance._enteringLandingCam)
        {
            if (!__instance.UsingLandingCam() && OWInput.IsNewlyPressed(InputLibrary.landingCamera, InputMode.All) && !OWInput.IsPressed(InputLibrary.freeLook, 0f))
            {
                __instance.EnterLandingView();
            }
            else if (__instance.UsingLandingCam() && (OWInput.IsNewlyPressed(InputLibrary.landingCamera, InputMode.All) || OWInput.IsNewlyPressed(InputLibrary.cancel, InputMode.All)))
            {
                InputLibrary.cancel.ConsumeInput();
                __instance.ExitLandingView();
            }
        }
        if (__instance.UsingLandingCam())
        {
            if (__instance._enteringLandingCam)
            {
                __instance.UpdateEnterLandingCamTransition();
            }
            if (!__instance._isLandingMode && __instance.IsLandingModeAvailable())
            {
                __instance.EnterLandingMode();
            }
            else if (__instance._isLandingMode && !__instance.IsLandingModeAvailable())
            {
                __instance.ExitLandingMode();
            }
            __instance._playerAttachOffset = Vector3.MoveTowards(__instance._playerAttachOffset, Vector3.zero, Time.deltaTime);
        }
        else
        {
            __instance._playerAttachOffset = __instance._thrusterModel.GetLocalAcceleration() / __instance._thrusterModel.GetMaxTranslationalThrust() * -0.2f;
            if (Locator.GetToolModeSwapper().GetToolMode() == ToolMode.None && OWInput.IsNewlyPressed(InputLibrary.cancel, InputMode.All))
            {
                __instance.ExitFlightConsole();
            }
        }
        __instance._playerAttachPoint.SetAttachOffset(__instance._playerAttachOffset);

        return false;
    }

    public static void SendAutopilotState(OWRigidbody body = null, bool destination = false,
        bool startMatch = false, bool stopMatch = false, bool abort = false)
    {
        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendAutopilotState(id, body, destination, startMatch, stopMatch, abort);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipCockpitUI), nameof(ShipCockpitUI.Update))]
    public static void UpdateAutopilotUI(ShipCockpitUI __instance)
    {
        if ((bool)enableEnhancedAutopilot.GetProperty() && SELocator.GetAutopilotPanelController() != null)
        {
            if (SELocator.GetAutopilotPanelController().IsAutopilotActive())
            {
                if (!__instance._autopilotLight.enabled)
                {
                    __instance._autopilotLight.enabled = true;
                    __instance._autopilotLightMaterial.SetColor(__instance._propID_EmissionColor, __instance._autopilotLightColor);
                }
            }
            else if (__instance._autopilotLight.enabled)
            {
                __instance._autopilotLight.enabled = false;
                __instance._autopilotLightMaterial.SetColor(__instance._propID_EmissionColor, 0f * __instance._autopilotLightColor);
            }

            if (__instance._autopilot.IsMatchingVelocity() || SELocator.GetAutopilotPanelController().IsPersistentInputActive())
            {
                if (!__instance._matchingVelocityLight.enabled)
                {
                    __instance._matchingVelocityLight.enabled = true;
                    __instance._matchVLightMaterial.SetColor(__instance._propID_EmissionColor, __instance._matchVLightColor);
                }
            }
            else if (__instance._matchingVelocityLight.enabled)
            {
                __instance._matchingVelocityLight.enabled = false;
                __instance._matchVLightMaterial.SetColor(__instance._propID_EmissionColor, 0f * __instance._matchVLightColor);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.IsLandingModeAvailable))]
    public static bool OverrideLandingModeAvailable(ref bool __result)
    {
        if ((bool)enableEnhancedAutopilot.GetProperty() && (SELocator.GetAutopilotPanelController().IsAutopilotActive()
            || SELocator.GetAutopilotPanelController().IsPersistentInputActive()))
        {
            __result = false;
            return false;
        }

        return true;
    }
    #endregion

    #region AlwaysEnableThrustIndicator
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ThrustAndAttitudeIndicator), nameof(ThrustAndAttitudeIndicator.Start))]
    public static void ThrustIndicatorStartOn(ThrustAndAttitudeIndicator __instance)
    {
        if (__instance._shipIndicatorMode && !__instance.enabled)
        {
            __instance._activeThrusterModel = __instance._shipThrusterModel;
            __instance._activeThrusterController = __instance._shipThrusterController;
            __instance._thrusterArrowRoot.gameObject.SetActive(true);
            __instance.enabled = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ThrustAndAttitudeIndicator), nameof(ThrustAndAttitudeIndicator.OnExitFlightConsole))]
    public static void KeepThrustIndicatorOn(ThrustAndAttitudeIndicator __instance)
    {
        if (__instance._shipIndicatorMode && !__instance.enabled)
        {
            __instance._activeThrusterModel = __instance._shipThrusterModel;
            __instance._activeThrusterController = __instance._shipThrusterController;
            __instance._thrusterArrowRoot.gameObject.SetActive(true);
            __instance.enabled = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ThrustAndAttitudeIndicator), nameof(ThrustAndAttitudeIndicator.OnEnterConversation))]
    public static void KeepThrustIndicatorOnInConversation(ThrustAndAttitudeIndicator __instance)
    {
        if (__instance._shipIndicatorMode && __instance._inConversation)
        {
            __instance._thrusterArrowRoot.gameObject.SetActive(true);
            __instance._inConversation = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ThrustAndAttitudeIndicator), nameof(ThrustAndAttitudeIndicator.LateUpdate))]
    public static void OverrideJetpackDisplay(ThrustAndAttitudeIndicator __instance)
    {
        if (__instance._shipIndicatorMode && __instance._jetpackThrusterModel.IsBoosterFiring())
        {
            Vector3 localAcceleration = __instance._activeThrusterModel.GetLocalAcceleration();
            float num3 = __instance._activeThrusterModel.GetMaxTranslationalThrust();
            __instance.DisplayArrows(0f, 1f, __instance._boostArrows, null);
            __instance.DisplayArrows(localAcceleration.y, num3, __instance._rendererDown, __instance._lightsDown);
        }
    }
    #endregion

    #region DisableHatch
    [HarmonyPrefix]
    [HarmonyPatch(typeof(HatchController), nameof(HatchController.OpenHatch))]
    public static bool StopHatchOpen()
    {
        return !(bool)disableHatch.GetProperty();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HatchController), nameof(HatchController.CloseHatch))]
    public static bool StopHatchClose()
    {
        return !(bool)disableHatch.GetProperty();
    }
    #endregion

    #region DisableLandingPads
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipDetachableLeg), nameof(ShipDetachableLeg.Detach))]
    public static void DisableLandingPadOnDetach(ShipDetachableLeg __instance, bool __runOriginal)
    {
        if (!__runOriginal) return;

        foreach (LandingPadSensor pad in __instance.GetComponentsInChildren<LandingPadSensor>())
        {
            pad.GetComponent<Collider>().enabled = false;
            pad._contactBody = null;
        }
    }
    #endregion

    #region CustomMenu
    public static bool ignoreNextMenuActivation = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Menu), nameof(Menu.Activate))]
    public static void RefreshMenuOnActivate(Menu __instance)
    {
        MenuManager menuManager = StartupPopupPatches.menuManager;
        IOptionsMenuManager OptionsMenuManager = menuManager.OptionsMenuManager;

        var menus = typeof(MenuManager).GetField("ModSettingsMenus", BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(menuManager)
            as List<(IModBehaviour behaviour, Menu modMenu)>;

        if (menus == null) return;

        for (int i = 0; i < menus.Count; i++)
        {
            if ((object)menus[i].behaviour == ShipEnhancements.Instance
                && menus[i].modMenu == __instance)
            {
                if (ignoreNextMenuActivation)
                {
                    return;
                }

                __instance.OnActivateMenu += () => ShipEnhancements.Instance.ModHelper.Events.Unity
                    .FireOnNextUpdate(() => SEMenuManager.RedrawSettingsMenu());
                return;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.ForceModOptionsOpen))]
    public static void PreventRepeatActivation(MenuManager __instance, bool force)
    {
        var menus = typeof(MenuManager).GetField("ModSettingsMenus", BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(__instance)
            as List<(IModBehaviour behaviour, Menu modMenu)>;

        if (menus == null) return;

        for (int i = 0; i < menus.Count; i++)
        {
            if ((object)menus[i].behaviour == ShipEnhancements.Instance)
            {
                ignoreNextMenuActivation = force;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(OWML.ModHelper.Menus.NewMenuSystem.Patches),
        nameof(OWML.ModHelper.Menus.NewMenuSystem.Patches.ResetToDefaultSettings))]
    public static void FixNumberValueReset()
    {
        MenuManager menuManager = StartupPopupPatches.menuManager;
        var menus = typeof(MenuManager).GetField("ModSettingsMenus", BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(menuManager)
            as List<(IModBehaviour behaviour, Menu modMenu)>;

        if (menus == null) return;

        Menu modMenu = null;
        for (int i = 0; i < menus.Count; i++)
        {
            if ((object)menus[i].behaviour == ShipEnhancements.Instance)
            {
                modMenu = menus[i].modMenu;
            }
        }

        if (modMenu == null || !modMenu.IsMenuEnabled()) return;

        var settings = ShipEnhancements.Instance.ModHelper.Config.Settings;
        var defaultSettings = ShipEnhancements.Instance.ModHelper.DefaultConfig;
        var options = modMenu.GetMenuOptions().Where(x => x.name != "UIElement-GammaButton").ToArray();

        for (var i = 0; i < options.Length; i++)
        {
            var menuOption = options[i];

            if (menuOption.TryGetComponent(out IOWMLTextEntryElement textEntry))
            {
                textEntry.SetCurrentValue(defaultSettings.GetSettingsValue<double>(textEntry.ModSettingKey).ToString(CultureInfo.InvariantCulture));
            }
        }

        ShipEnhancements.Instance.ModHelper.Events.Unity.FireOnNextUpdate(SettingExtensions.ResetCustomSettings);
    }
    #endregion

    #region ShipRF
    public static bool SkipNextRFUpdate = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ReferenceFrameTracker), nameof(ReferenceFrameTracker.TargetReferenceFrame))]
    public static void StoreShipRF(ReferenceFrameTracker __instance, ReferenceFrame frame)
    {
        if (!(bool)splitLockOn.GetProperty()) return;

        if (SkipNextRFUpdate)
        {
            SkipNextRFUpdate = false;
            return;
        }
        SELocator.OnTargetReferenceFrame(frame);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ReferenceFrameTracker), nameof(ReferenceFrameTracker.UntargetReferenceFrame), typeof(bool))]
    public static void RemoveShipRF(ReferenceFrameTracker __instance)
    {
        if (!(bool)splitLockOn.GetProperty()) return;

        if (SkipNextRFUpdate)
        {
            SkipNextRFUpdate = false;
            return;
        }
        SELocator.OnUntargetReferenceFrame();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.OnPressInteract))]
    public static void SwitchToShipRF()
    {
        if (!(bool)splitLockOn.GetProperty()) return;

        SELocator.OnEnterShip();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.ExitFlightConsole))]
    public static void SwitchToPlayerRF()
    {
        if (!(bool)splitLockOn.GetProperty()) return;

        SELocator.OnExitShip();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.OnUntargetReferenceFrame))]
    public static bool KeepAutopilotOnRemoveRF(ShipCockpitController __instance)
    {
        if (!(bool)splitLockOn.GetProperty()) return true;

        if (SELocator.GetReferenceFrame() == null)
        {
            return true;
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.OnTargetReferenceFrame))]
    public static bool KeepAutopilotOnSwitchRF(ShipCockpitController __instance, ReferenceFrame referenceFrame)
    {
        if (!(bool)splitLockOn.GetProperty()) return true;

        if (PlayerState.AtFlightConsole() && SELocator.GetReferenceFrame() != referenceFrame)
        {
            return true;
        }

        return false;
    }
    #endregion

    #region ShipLightFix
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipLight), nameof(ShipLight.SetExtraIntensityScale))]
    public static void UpdateShipLightEmission(ShipLight __instance)
    {
        if (__instance._emissiveRenderer != null)
        {
            float num2 = __instance._light.intensity / __instance._baseIntensity;
            __instance._matPropBlock.SetColor(__instance._propID_EmissionColor, __instance._baseEmission * num2);
            __instance._emissiveRenderer.SetPropertyBlock(__instance._matPropBlock);
        }
    }
    #endregion

    #region TotallyNotSuspiciousPatches
    [HarmonyPrefix]
    [HarmonyPatch(typeof(NomaiTranslatorProp), nameof(NomaiTranslatorProp.DisplayTextNode))]
    public static bool HideErnestonianText(NomaiTranslatorProp __instance)
    {
        bool flag = __instance._scanBeams[0]._nomaiTextLine != null && __instance._scanBeams[0]._nomaiTextLine
            .GetComponentInParent<NomaiText>() is ErnestonianText;

        if (flag && !PlayerData.GetPersistentCondition("SE_HAS_ERNESTONIAN_TRANSLATOR"))
        {
            __instance._textField.text = UITextLibrary.GetString(UITextType.TranslatorUntranslatableWarning);

            if (!PlayerData.GetPersistentCondition("SE_FOUND_ERNESTONIAN_TEXT"))
            {
                PlayerData.SetPersistentCondition("SE_FOUND_ERNESTONIAN_TEXT", true);
            }

            return false;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NomaiTranslatorProp), nameof(NomaiTranslatorProp.DisplayTextNode))]
    public static void ChangeErnestonianUnreadMessage(NomaiTranslatorProp __instance)
    {
        if (!PlayerData.GetPersistentCondition("SE_HAS_ERNESTONIAN_TRANSLATOR"))
        {
            return;
        }

        bool flag = __instance._scanBeams[0]._nomaiTextLine != null && __instance._scanBeams[0]._nomaiTextLine
            .GetComponentInParent<NomaiText>() is ErnestonianText;

        if (flag && __instance._translationTimeElapsed == 0f
            && !__instance._nomaiTextComponent.IsTranslated(__instance._currentTextID))
        {
            __instance._textField.text = TranslationHandler.GetTranslation("<!> Untranslated Ernestonian writing <!>", TranslationHandler.TextType.UI);
        }
    }
    #endregion

    #region InheritableForcesFix
    public static readonly HashSet<ForceDetector> seenDetectors = new();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ForceDetector), nameof(ForceDetector.AccumulateAcceleration))]
    public static bool InheritableForcesFix_AccumulateAcceleration_Dynamic(ForceDetector __instance)
    {
        if (__instance is not DynamicForceDetector) return true;

        bool clearDetectors = false;

        __instance._netAcceleration = Vector3.zero;
        for (int i = __instance._activeVolumes.Count - 1; i >= 0; i--)
        {
            if (__instance._activeVolumes[i] == null)
            {
                Debug.LogError("what", __instance.gameObject);
                Debug.Break();
            }
            __instance._netAcceleration += (__instance._activeVolumes[i] as ForceVolume).CalculateForceAccelerationOnBody(__instance._attachedBody) * __instance._fieldMultiplier;
        }
        if (__instance._activeInheritedDetector != null && !seenDetectors.Contains(__instance._activeInheritedDetector))
        {
            if (seenDetectors.Count == 0)
            {
                clearDetectors = true;
            }
            seenDetectors.Add(__instance);
            __instance._netAcceleration += __instance._activeInheritedDetector.GetForceAcceleration();
        }

        if (clearDetectors)
        {
            seenDetectors.Clear();
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AlignmentForceDetector), nameof(AlignmentForceDetector.AccumulateAcceleration))]
    public static bool InheritableForcesFix_AccumulateAcceleration_Alignment(AlignmentForceDetector __instance)
    {
        if (Keyboard.current.mKey.isPressed)
        {
            ShipEnhancements.WriteDebugMessage((__instance is AlignmentForceDetector) + " - " + __instance);
        }

        bool clearDetectors = false;

        var detector = __instance as AlignmentForceDetector;
        detector._netAcceleration = Vector3.zero;
        detector._aligmentAccel = Vector3.zero;
        detector._alignmentAngularVelocity = Vector3.zero;
        int num = 0;
        for (int i = detector._activeVolumes.Count - 1; i >= 0; i--)
        {
            ForceVolume forceVolume = detector._activeVolumes[i] as ForceVolume;
            Vector3 vector = forceVolume.CalculateForceAccelerationOnBody(detector._attachedBody);
            detector._netAcceleration += vector * detector._fieldMultiplier;
            int alignmentPriority = forceVolume.GetAlignmentPriority();
            if (forceVolume.GetAffectsAlignment(detector._attachedBody))
            {
                if (alignmentPriority > num)
                {
                    detector._aligmentAccel = vector;
                    detector._alignmentAngularVelocity = forceVolume.GetAttachedOWRigidbody().GetAngularVelocity();
                    num = alignmentPriority;
                }
                else if (alignmentPriority == num)
                {
                    detector._aligmentAccel += vector;
                    detector._alignmentAngularVelocity += forceVolume.GetAttachedOWRigidbody().GetAngularVelocity();
                }
            }
        }
        if (Keyboard.current.nKey.isPressed)
        {
            ShipEnhancements.WriteDebugMessage("Process on " + __instance.GetAttachedOWRigidbody());
            ShipEnhancements.WriteDebugMessage("Seen detectors: ");
            foreach (var d in seenDetectors)
            {
                ShipEnhancements.WriteDebugMessage(d);
            }
        }
        if (detector._activeInheritedDetector != null && !seenDetectors.Contains(detector._activeInheritedDetector))
        {
            if (seenDetectors.Count == 0)
            {
                clearDetectors = true;
            }
            if (Keyboard.current.nKey.isPressed)
            {
                ShipEnhancements.WriteDebugMessage("Add " + __instance);
            }
            seenDetectors.Add(__instance);
            detector._netAcceleration += detector._activeInheritedDetector.GetForceAcceleration();
        }
        else if (Keyboard.current.nKey.isPressed)
        {
            ShipEnhancements.WriteDebugMessage("Cancel");
        }

        if (clearDetectors)
        {
            seenDetectors.Clear();
        }

        return false;
    }
    #endregion

    #region EjectSpeedMultiplier
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipEjectionSystem), nameof(ShipEjectionSystem.FixedUpdate))]
    public static bool AddEjectSpeedMultiplier(ShipEjectionSystem __instance)
    {
        float multiplier = ShipEnhancements.ExperimentalSettings?.Eject_SpeedMultiplier ?? 1f;
        if (multiplier == 1f) return true;

        if (__instance._ejectPressed)
        {
            OWRigidbody owrigidbody = __instance._cockpitModule.Detach();
            __instance._shipBody.transform.position -= __instance._shipBody.transform.forward * 2f;
            float num = __instance._ejectImpulse;
            num *= multiplier;
            if (Locator.GetShipDetector().GetComponent<ShipFluidDetector>().InOceanBarrierZone())
            {
                MonoBehaviour.print("Ship in ocean barrier zone, reducing eject impulse.");
                num = 1f;
            }
            __instance._shipBody.AddLocalImpulse(Vector3.back * num);
            owrigidbody.AddLocalImpulse(Vector3.forward * num);
            __instance._audioController.PlayEject();
            RumbleManager.PulseEject();
            __instance.enabled = false;
        }

        return false;
    }
    #endregion

    #region QuantumShip
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.CastForGrounded))]
    public static void UpdateShipGroundedPre(PlayerCharacterController __instance)
    {
        if (!(bool)enableQuantumShip.GetProperty() 
            && !(ShipEnhancements.ExperimentalSettings?.UltraQuantumShip ?? false)) return;

        if (!PatchHandler.CollidingWithShip && __instance._groundCollider != null
            && __instance._groundCollider.GetComponentInParent<ShipBody>())
        {
            PatchHandler.SetPlayerStandingOnShip(true);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerCharacterController), nameof(PlayerCharacterController.UpdateGrounded))]
    public static void UpdateShipGroundedPost(PlayerCharacterController __instance)
    {
        if (!(bool)enableQuantumShip.GetProperty()
            && !(ShipEnhancements.ExperimentalSettings?.UltraQuantumShip ?? false)) return;

        if (__instance._isMovementLocked || __instance._isTumbling || !__instance._isAlignedToForce)
        {
            return;
        }

        if (PatchHandler.CollidingWithShip)
        {
            PatchHandler.SetPlayerStandingOnShip(false);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(QuantumMoon), nameof(QuantumMoon.SetSurfaceState))]
    public static void FixShipStateChange(QuantumMoon __instance)
    {
        if ((bool)enableQuantumShip.GetProperty() && __instance._isShipInside)
        {
            SELocator.GetShipBody().SetVelocity(__instance._moonBody.GetPointVelocity(SELocator.GetShipTransform().position));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SocketedQuantumObject), nameof(SocketedQuantumObject.MoveToSocket))]
    public static void FixShipSocketChange(SocketedQuantumObject __instance)
    {
        if ((bool)enableQuantumShip.GetProperty() && __instance is SocketedQuantumShip)
        {
            QuantumMoon qm = Locator.GetQuantumMoon();
            if (qm == null) return;
            SELocator.GetShipBody().SetVelocity(qm._moonBody.GetPointVelocity(SELocator.GetShipTransform().position));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QuantumObject), nameof(QuantumObject.IsLockedByActiveCamera))]
    public static void ShipVisibilityLockFix(QuantumObject __instance, ref bool __result)
    {
        if (!(bool)enableQuantumShip.GetProperty() || __instance is not SocketedQuantumShip) return;
        __result = __result || PlayerState.IsInsideShip();
    }
    #endregion

    #region GasLeak
    [HarmonyPostfix]
    [HarmonyPatch(typeof(OWTime), nameof(OWTime.Pause))]
    public static void GasLeakPause()
    {
        if ((bool)enableGasLeak.GetProperty() && UnityEngine.Random.value < 0.001f)
        {
            Time.timeScale = 1f;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FirstPersonManipulator), nameof(FirstPersonManipulator.LateUpdate))]
    public static void UpdateLastFocusedRepairReceiver(FirstPersonManipulator __instance)
    {
        if ((bool)enableGasLeak.GetProperty())
        {
            PatchHandler.SetLastFocusedRepairReciver(__instance._focusedRepairReceiver);
        }
    }
    #endregion

    #region ShipSignalscopeZoom

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Signalscope), nameof(Signalscope.Update))]
    public static bool ShipSigScopeZoom(Signalscope __instance)
    {
        if (!(bool)enableShipSignalscopeZoom.GetProperty()) return true;

        PlayerTool_Update(__instance);
        if (!__instance._isEquipped || __instance._isPuttingAway)
        {
            return false;
        }

        __instance.UpdateAudioSignals();
        if (Locator.GetPlayerSuit().IsWearingHelmet() && !__instance._inZoomMode &&
            __instance._signalscopeUI.IsWaveformActive())
        {
            __instance._signalscopeUI.SetWaveformUIActive(false);
        }
        else if ((__instance._inZoomMode ||
                (!Locator.GetPlayerSuit().IsWearingHelmet() && !PlayerState.AtFlightConsole())) &&
            !__instance._signalscopeUI.IsWaveformActive())
        {
            __instance._signalscopeUI.SetWaveformUIActive(true);
        }

        if (!__instance._isPuttingAway && !__instance._inZoomMode &&
            (OWInput.IsInputMode(InputMode.Character) ||
                OWInput.IsInputMode(InputMode.ScopeZoom | InputMode.ShipCockpit)))
        {
            if (OWInput.IsNewlyPressed(InputLibrary.toolActionPrimary, InputMode.All))
            {
                __instance.EnterSignalscopeZoom();
            }
        }
        else if (__instance._inZoomMode)
        {
            float num = OWInput.GetValue(InputLibrary.toolOptionDown, InputMode.ScopeZoom | InputMode.ShipCockpit) -
                OWInput.GetValue(InputLibrary.toolOptionUp, InputMode.ScopeZoom | InputMode.ShipCockpit);
            float num2 = (__instance._useWarpFov ? (__instance._targetWarpFOV - __instance._targetFOV) : 0f);
            __instance._fovWarpOffset = Mathf.MoveTowards(__instance._fovWarpOffset, num2, Time.deltaTime * 30f);
            __instance._targetFOV = Mathf.Clamp(__instance._targetFOV + num * Time.deltaTime * 50f, 10f, 60f);
            __instance._playerCamController.SetTargetFieldOfView(__instance._targetFOV + __instance._fovWarpOffset);
            if (OWInput.IsNewlyReleased(InputLibrary.toolActionPrimary, InputMode.ScopeZoom | InputMode.ShipCockpit))
            {
                __instance.ExitSignalscopeZoom();
            }
        }

        int frequencyFilterIndex = __instance._frequencyFilterIndex;
        if (!PlayerState.IsInsideTheEye() &&
            OWInput.IsInputMode(InputMode.Character | InputMode.ScopeZoom | InputMode.ShipCockpit))
        {
            if (OWInput.IsNewlyPressed(InputLibrary.toolOptionRight, InputMode.All))
            {
                __instance.SwitchFrequencyFilter(1);
            }
            else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionLeft, InputMode.All))
            {
                __instance.SwitchFrequencyFilter(-1);
            }
        }

        if (__instance._frequencyFilterIndex != frequencyFilterIndex)
        {
            __instance._signalscopeAudio.PlayChangeSignalscopeFrequency();
        }

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Signalscope), nameof(Signalscope.ExitSignalscopeZoom))]
    public static void PreventScopeObjEnable(Signalscope __instance)
    {
        if (!(bool)enableShipSignalscopeZoom.GetProperty() || !PlayerState.AtFlightConsole()) return;
        
        __instance._scopeGameObject.SetActive(false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.AllowFreeLook))]
    public static void DisableFreeLookWhileZoomed(ShipCockpitController __instance, ref bool __result)
    {
        if (!(bool)enableShipSignalscopeZoom.GetProperty()) return;
        
        __result = __result && !Locator.GetPlayerController()._signalscopeZoom;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipThrusterController), nameof(ShipThrusterController.ReadRotationalInput))]
    public static void AddLookSensitivityFactorWhileZoomed(ShipThrusterController __instance, ref Vector3 __result)
    {
        if (!(bool)enableShipSignalscopeZoom.GetProperty()) return;
        
        if (PlayerState.AtFlightConsole() && Locator.GetPlayerController()._signalscopeZoom)
        {
            __result *= PlayerCameraController.ZOOM_SCALAR;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BaseInputManager), nameof(BaseInputManager.OnEnterSignalscopeZoom))]
    public static bool DisableScopeZoomModeAtCockpit(BaseInputManager __instance)
    {
        if (!(bool)enableShipSignalscopeZoom.GetProperty()) return true;
        
        return !PlayerState.AtFlightConsole();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BaseInputManager), nameof(BaseInputManager.OnExitSignalscopeZoom))]
    public static bool DisableModeResetAfterZoom(BaseInputManager __instance)
    {
        if (!(bool)enableShipSignalscopeZoom.GetProperty()) return true;
        
        return !PlayerState.AtFlightConsole();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerCameraController), nameof(PlayerCameraController.UpdateInput))]
    public static bool ForceShipControlWhileZoomed(PlayerCameraController __instance)
    {
        if (!(bool)enableShipSignalscopeZoom.GetProperty()) return true;
        
        bool usingFreeLook = __instance._shipController != null && __instance._shipController.AllowFreeLook() &&
            OWInput.IsPressed(InputLibrary.freeLook, 0f);
        bool inCharacterMode = OWInput.IsInputMode(InputMode.Character | InputMode.ScopeZoom |
            InputMode.NomaiRemoteCam | InputMode.PatchingSuit);
        bool usingCockpitZoom = PlayerState.AtFlightConsole() && __instance._zoomed;
        if (__instance._isSnapping || __instance._isLockedOn ||
            (PlayerState.InZeroG() && PlayerState.IsWearingSuit()) ||
            (!inCharacterMode && !usingFreeLook) ||
            usingCockpitZoom)
        {
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.Update))]
    public static bool PrevetProbeEquipFromSigScope(ToolModeSwapper __instance)
    {
        if (!(bool)enableShipSignalscopeZoom.GetProperty()) return true;

        if (__instance._isSwitchingToolMode && !__instance._equippedTool.IsEquipped())
        {
            __instance._equippedTool = __instance._nextTool;
            __instance._nextTool = null;
            if (__instance._equippedTool != null)
            {
                __instance._equippedTool.EquipTool();
            }

            __instance._currentToolMode = __instance._nextToolMode;
            __instance._nextToolMode = ToolMode.None;
            __instance._isSwitchingToolMode = false;
        }

        InputMode inputMode = InputMode.Character | InputMode.ShipCockpit;
        if (!__instance.IsNomaiTextInFocus())
        {
            __instance._waitForLoseNomaiTextFocus = false;
        }

        if (__instance._shipDestroyed && __instance._currentToolGroup == ToolGroup.Ship)
        {
            return false;
        }

        if (__instance._currentToolMode != ToolMode.None && __instance._currentToolMode != ToolMode.Item &&
            (OWInput.IsNewlyPressed(InputLibrary.cancel, inputMode | InputMode.ScopeZoom) ||
                PlayerState.InConversation()))
        {
            InputLibrary.cancel.ConsumeInput();
            if (__instance.GetAutoEquipTranslator() && __instance._currentToolMode == ToolMode.Translator)
            {
                __instance._waitForLoseNomaiTextFocus = true;
            }

            __instance.UnequipTool();
        }
        else if (__instance.IsNomaiTextInFocus() && __instance._currentToolMode != ToolMode.Translator &&
            ((__instance.GetAutoEquipTranslator() && !__instance._waitForLoseNomaiTextFocus) ||
                OWInput.IsNewlyPressed(InputLibrary.interact, inputMode)))
        {
            __instance.EquipToolMode(ToolMode.Translator);
            if (__instance._firstPersonManipulator.GetFocusedNomaiText() != null && __instance._firstPersonManipulator
                .GetFocusedNomaiText().CheckTurnOffFlashlight())
            {
                Locator.GetFlashlight().TurnOff(false);
            }
        }
        else if (__instance._currentToolMode == ToolMode.Translator && !__instance.IsNomaiTextInFocus() &&
            __instance.GetAutoEquipTranslator())
        {
            __instance.UnequipTool();
        }
        else if (OWInput.IsNewlyPressed(InputLibrary.probeLaunch, inputMode))
        {
            if (__instance._currentToolGroup == ToolGroup.Suit &&
                __instance._itemCarryTool.GetHeldItemType() == ItemType.DreamLantern)
            {
                return false;
            }

            if (((__instance._currentToolMode == ToolMode.None || __instance._currentToolMode == ToolMode.Item) &&
                    Locator.GetPlayerSuit().IsWearingSuit(false)) ||
                ((__instance._currentToolMode ==
                        ToolMode.None /*|| __instance._currentToolMode == ToolMode.SignalScope*/) &&
                    OWInput.IsInputMode(InputMode.ShipCockpit)))
            {
                __instance.EquipToolMode(ToolMode.Probe);
            }
        }
        else if (OWInput.IsNewlyPressed(InputLibrary.signalscope, inputMode | InputMode.ScopeZoom))
        {
            if (PlayerState.InDreamWorld())
            {
                return false;
            }

            if (__instance._currentToolMode == ToolMode.SignalScope)
            {
                __instance.UnequipTool();
            }
            else
            {
                __instance.EquipToolMode(ToolMode.SignalScope);
            }
        }

        bool flag = __instance._itemCarryTool.UpdateInteract(__instance._firstPersonManipulator,
            __instance.IsItemToolBlocked());
        if (!__instance._itemCarryTool.IsEquipped() && flag)
        {
            __instance.EquipToolMode(ToolMode.Item);
            return false;
        }

        if (__instance._itemCarryTool.GetHeldItem() != null && __instance._currentToolMode == ToolMode.None &&
            OWInput.IsInputMode(InputMode.Character) && !OWInput.IsChangePending())
        {
            __instance.EquipToolMode(ToolMode.Item);
        }

        return false;
    }

    #endregion
    
    #region DisableSignalscopeBrackets

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SignalscopeReticleController), nameof(SignalscopeReticleController.UpdateBrackets))]
    public static bool DisableSignalscopeBrackets(SignalscopeReticleController __instance)
    {
        if (!(bool)disableSignalscopeBrackets.GetProperty()) return true;
        
        if (__instance._clonedLeftBrackets.Count > 0)
        {
            __instance._clonedLeftBrackets.Clear();
            __instance._clonedRightBrackets.Clear();
        }
        return false;
    }
    
    #endregion

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipAudioController), nameof(ShipAudioController.PlayEject))]
    public static bool FixEjectAudio(ShipAudioController __instance)
    {
        __instance.transform.Find("ShipInteriorAudio/EjectAudio").GetComponent<OWAudioSource>()
            .PlayOneShot(AudioType.ShipCockpitEject);
        return false;
    }
}