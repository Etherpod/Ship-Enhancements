using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

[HarmonyPatch]
public static class PatchClass
{
    public static float baseRoastingStickMaxZ = 0f;

    #region DisableHeadlights
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.UpdateShipLightInput))]
    public static bool DisableHeadlights(ShipCockpitController __instance)
    {
        if ((bool)disableHeadlights.GetProperty()) return false;
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
    public static bool KeepHelmetOnAtCockpit(ShipCockpitController __instance)
    {
        if (!(bool)keepHelmetOn.GetProperty() || !ShipEnhancements.Instance.oxygenDepleted) return true;

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
        if (!(bool)zeroGravityCockpitFreeLook.GetProperty()) return true;

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
        __instance._currentOxygen = Mathf.Max(__instance._currentOxygen - (amount * (float)oxygenDrainMultiplier.GetProperty()), 0f);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipResources), nameof(ShipResources.DrainFuel))]
    public static bool ApplyFuelDrainMultiplier(ShipResources __instance, float amount)
    {
        __instance._currentFuel = Mathf.Max(__instance._currentFuel - (amount * (float)fuelDrainMultiplier.GetProperty()), 0f);
        return false;
    }
    #endregion

    #region DamageMultiplier
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipHull), nameof(ShipHull.FixedUpdate))]
    public static bool ApplyHullDamageMultiplier(ShipHull __instance)
    {
        if ((float)shipDamageMultiplier.GetProperty() == 1 && (float)shipDamageSpeedMultiplier.GetProperty() == 1)
        {
            return true;
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
            float num = Mathf.InverseLerp(30f * (float)shipDamageSpeedMultiplier.GetProperty(),
                200f * (float)shipDamageSpeedMultiplier.GetProperty(), __instance._dominantImpact.speed);
            if (num > 0f)
            {
                float num2 = 0.15f;
                if (num < num2 && __instance._integrity > 1f - num2)
                {
                    num = num2;
                }
                num *= (float)shipDamageMultiplier.GetProperty();
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
        if ((float)shipDamageMultiplier.GetProperty() == 1 && (float)shipDamageSpeedMultiplier.GetProperty() == 1)
        {
            return true;
        }

        if (__instance._damaged)
        {
            return false;
        }
        if (UnityEngine.Random.value / (float)shipDamageMultiplier.GetProperty()
            < __instance._damageProbabilityCurve.Evaluate(impact.speed / (float)shipDamageSpeedMultiplier.GetProperty()))
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
        if (!(bool)disableShipRepair.GetProperty())
        {
            for (int j = 0; j < __instance._shipComponents.Length; j++)
            {
                if (__instance._shipComponents[j].isDamaged && __instance._shipComponents[j].componentName != UITextType.ShipPartGravity
                    && __instance._shipComponents[j].componentName != UITextType.ShipPartReactor
                    && !((bool)disableLandingCamera.GetProperty() && __instance._shipComponents[j].componentName == UITextType.ShipPartCamera)
                    && !((bool)disableHeadlights.GetProperty() && __instance._shipComponents[j].componentName == UITextType.ShipPartLights))
                {
                    flag = true;
                }
            }
        }
        __result = flag;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipDamageController), nameof(ShipDamageController.OnImpact))]
    public static bool ApplyExplosionDamageMultiplier(ShipDamageController __instance, ImpactData impact)
    {
        if ((float)shipDamageMultiplier.GetProperty() == 1 && (float)shipDamageSpeedMultiplier.GetProperty() == 1)
        {
            return true;
        }

        if (impact.otherCollider.attachedRigidbody != null && impact.otherCollider.attachedRigidbody.CompareTag("Player") && PlayerState.IsInsideShip())
        {
            return false;
        }
        if (impact.speed >= 300f * (float)shipDamageSpeedMultiplier.GetProperty() / ((float)shipDamageMultiplier.GetProperty() / 10) && !__instance._exploded)
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
        if ((bool)disableShipOxygen.GetProperty() || !(bool)shipOxygenRefill.GetProperty()
            || ModCompatibility.GetModSetting("Stonesword.ResourceManagement", "Enable Oxygen Refill")) return true;

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
                __instance.AddOxygen(100f * Time.deltaTime * (float)oxygenRefillMultiplier.GetProperty());
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
        if (!(bool)disableShipRepair.GetProperty()) return;

        __instance._repairReceiver._repairDistance = 0f;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipHull), nameof(ShipHull.Start))]
    public static void DisableShipHullRepair(ShipHull __instance)
    {
        if (!(bool)disableShipRepair.GetProperty()) return;

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
        if (!(bool)enableGravityLandingGear.GetProperty()) return;
        __instance.gameObject.AddComponent<GravityLandingGear>();
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
        if (!(bool)enableThrustModulator.GetProperty()) return;
        __result *= ShipEnhancements.Instance.thrustModulatorLevel / 5f;
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

        float multiplier = ShipEnhancements.Instance.thrustModulatorLevel / 5f;

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
        TemperatureZone tempZone = __instance.transform.Find("Sector_SUN/Volumes_SUN").GetComponentInChildren<TemperatureZone>();
        if (tempZone != null)
        {
            tempZone.SetScale(scale);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SupernovaEffectController), nameof(SupernovaEffectController.FixedUpdate))]
    public static void UpdateSupernovaTempZone(SupernovaEffectController __instance)
    {
        TemperatureZone tempZone = __instance.GetComponentInChildren<TemperatureZone>();
        if (tempZone != null)
        {
            tempZone.SetScale(__instance._currentSupernovaScale);
        }
    }
    #endregion

    #region DisableReferenceFrame
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ReferenceFrameTracker), nameof(ReferenceFrameTracker.Update))]
    public static bool DisableReferenceFrame(ReferenceFrameTracker __instance)
    {
        if (!(bool)disableReferenceFrame.GetProperty()) return true;

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
    #endregion

    #region DisableHUDMarkers
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CanvasMarker), nameof(CanvasMarker.SetVisibility))]
    public static bool DisableHUDMarker(bool value)
    {
        if (!(bool)disableMapMarkers.GetProperty()) return true;

        if (value && (ShipLogEntryHUDMarker.s_entryLocation == null || !ShipLogEntryHUDMarker.s_entryLocation.IsWithinCloakField()
            || (ShipLogEntryHUDMarker.s_entryLocation.IsWithinCloakField() && Locator.GetCloakFieldController().isPlayerInsideCloak)))
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
        if (__instance.GetComponent<ShipLogEntryHUDMarker>() && !Locator.GetCloakFieldController().isPlayerInsideCloak
            && ShipLogEntryHUDMarker.s_entryLocation != null && ShipLogEntryHUDMarker.s_entryLocation.IsWithinCloakField())
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
        if (__instance.GetComponent<ShipLogEntryHUDMarker>() && !Locator.GetCloakFieldController().isPlayerInsideCloak
            && ShipLogEntryHUDMarker.s_entryLocation != null && ShipLogEntryHUDMarker.s_entryLocation.IsWithinCloakField())
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
        if (ShipLogEntryHUDMarker.s_entryLocation != null
            && ShipLogEntryHUDMarker.s_entryLocation.IsWithinCloakField() && Locator.GetCloakFieldController().isPlayerInsideCloak)
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
    public static void ActivateHatchTractorBeam(HatchController __instance)
    {
        if (!(bool)enableAutoHatch.GetProperty()) return;

        if (!__instance.IsPlayerInShip())
        {
            Locator.GetShipBody().GetComponentInChildren<ShipTractorBeamSwitch>().ActivateTractorBeam();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipTractorBeamSwitch), nameof(ShipTractorBeamSwitch.OnTriggerExit))]
    public static bool CloseHatchOutsideShip(ShipTractorBeamSwitch __instance)
    {
        if (!(bool)enableAutoHatch.GetProperty()) return true;

        HatchController hatch = Locator.GetShipBody().GetComponentInChildren<HatchController>();
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

    #region AngularDrag
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ThrusterModel), nameof(ThrusterModel.Awake))]
    public static void RemoveAngularDrag(ThrusterModel __instance)
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
    public static void RemoveFluidAngularDrag(FluidDetector __instance)
    {
        // Only runs when in fluid
        if (__instance.CompareTag("ShipDetector"))
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
                return false;
            }
        }
        if (shipLauncher)
        {
            if (launchingDisabled && usingShip)
            {
                return false;
            }
            if (scoutLauncherComponent && SELocator.GetProbeLauncherComponent().isDamaged)
            {
                return false;
            }
            if (recallDisabled && !ShipProbePickupVolume.probeInShip)
            {
                return false;
            }
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UITextLibrary), nameof(UITextLibrary.GetString))]
    public static void ReturnProbeLauncherName(UITextType TextID, ref string __result)
    {
        if (TextID == ShipEnhancements.Instance.probeLauncherName)
        {
            __result = "SCOUT LAUNCHER";
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipHull), nameof(ShipHull.Awake))]
    public static void AddScoutLauncherComponent(ShipHull __instance)
    {
        if (!(bool)enableScoutLauncherComponent.GetProperty()) return;

        if (__instance.hullName != UITextType.ShipPartForward) return;
        GameObject probeLauncherComponent = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ProbeLauncherComponent.prefab");
        GameObject componentObj = UnityEngine.Object.Instantiate(probeLauncherComponent, 
            __instance.GetComponentInParent<ShipBody>().GetComponentInChildren<PlayerProbeLauncher>().transform.parent);
        AssetBundleUtilities.ReplaceShaders(componentObj);
        SELocator.SetProbeLauncherComponent(componentObj.GetComponent<ProbeLauncherComponent>());
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
            ShipEnhancements.WriteDebugMessage("unga bunga");
            __instance._activeProbe = Locator.GetProbe();
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
            __instance._activeProbe = Locator.GetProbe();
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
        }
        else
        {
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
                Locator.GetShipTransform().GetComponentInChildren<ShipProbePickupVolume>().RecallProbeFromShip();
                return;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.AllowPhotoMode))]
    public static void UpdateAllowPhotoMode(ProbeLauncher __instance, ref bool __result)
    {
        if (!(bool)disableScoutLaunching.GetProperty() && !(bool)disableScoutRecall.GetProperty()
            && !(bool)enableManualScoutRecall.GetProperty() && !(bool)enableScoutLauncherComponent.GetProperty())
        {
            return;
        }

        if (__result && ShipProbePickupVolume.probeInShip)
        {
            __result = false;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.UpdatePreLaunch))]
    public static bool DisablePhotoMode(ProbeLauncher __instance)
    {
        bool recallOrLaunchingDisabled = (bool)disableScoutRecall.GetProperty() || (bool)disableScoutLaunching.GetProperty();
        bool manualScoutRecall = (bool)enableManualScoutRecall.GetProperty();
        bool scoutLauncherComponent = (bool)enableScoutLauncherComponent.GetProperty();

        if (!recallOrLaunchingDisabled && !manualScoutRecall && !scoutLauncherComponent)
        {
            return true;
        }

        if (recallOrLaunchingDisabled && ShipProbePickupVolume.probeInShip)
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

        if (!(bool)enableShipItemPlacement.GetProperty()) return true;

        PlayerCharacterController playerController = Locator.GetPlayerController();
        if (!playerController.IsGrounded() || PlayerState.IsAttached()/* || PlayerState.IsInsideShip()*/)
        {
            __result = false;
            return false;
        }
        if (__instance._heldItem != null && !__instance._heldItem.CheckIsDroppable())
        {
            __result = false;
            return false;
        }
        if (playerController.GetRelativeGroundVelocity().sqrMagnitude >= playerController.GetRunSpeedMagnitude() * playerController.GetRunSpeedMagnitude())
        {
            __result = false;
            return false;
        }
        Vector3 forward = Locator.GetPlayerTransform().forward;
        Vector3 forward2 = Locator.GetPlayerCamera().transform.forward;
        float num = Vector3.Angle(forward, forward2);
        float num2 = Mathf.InverseLerp(0f, 70f, num);
        float num3 = 2.5f;
        if (num2 <= 1f)
        {
            num3 = Mathf.Lerp(2.5f, 4f, num2);
        }
        if (Physics.Raycast(Locator.GetPlayerCamera().transform.position, forward2, out hit, num3, OWLayerMask.physicalMask | OWLayerMask.interactMask))
        {
            if (OWLayerMask.IsLayerInMask(hit.collider.gameObject.layer, OWLayerMask.interactMask))
            {
                __result = false;
                return false;
            }
            if (Vector3.Angle(Locator.GetPlayerTransform().up, hit.normal) <= __instance._maxDroppableSlopeAngle)
            {
                IgnoreCollision component = hit.collider.GetComponent<IgnoreCollision>();
                if (component == null || !component.PreventsItemDrop())
                {
                    targetRigidbody = hit.collider.GetAttachedOWRigidbody(false);
                    /*if (targetRigidbody.gameObject.CompareTag("Ship"))
                    {
                        return false;
                    }*/
                    dropTarget = hit.collider.GetComponentInParent<IItemDropTarget>();
                    __result = true;
                    return false;
                }
            }
        }
        __result = false;
        return false;
    }
    #endregion

    #region PortableCampfire
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
                    if (campfire.IsExtinguished())
                    {
                        Locator.GetPromptManager().RemoveScreenPrompt(campfire.GetPrompt(), PromptPosition.Center);
                        campfire.PackUp();
                    }
                    else
                    {
                        campfire.SetState(Campfire.State.UNLIT);
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
    #endregion

    #region ExplosionMultiplier
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExplosionController), nameof(ExplosionController.Update))]
    public static bool FixExplosionHeatVolume(ExplosionController __instance)
    {
        if ((float)shipExplosionMultiplier.GetProperty() == 1f) return true;

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
        __instance.GetComponent<SphereCollider>().radius = Mathf.Lerp(0.1f, 1f, num * 2f);
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
    #endregion

    #region PersistentInput
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ThrustAndAttitudeIndicator), nameof(ThrustAndAttitudeIndicator.OnExitFlightConsole))]
    public static bool KeepThrustIndicatorOn(ThrustAndAttitudeIndicator __instance)
    {
        if (!(bool)enablePersistentInput.GetProperty() || !Locator.GetShipBody().GetComponent<ShipPersistentInput>().InputEnabled()) return true;

        __instance._activeThrusterModel = __instance._jetpackThrusterModel;
        __instance._activeThrusterController = __instance._jetpackThrusterController;
        if (__instance._shipIndicatorMode)
        {
            //__instance.ResetAllArrows();
            //__instance._thrusterArrowRoot.gameObject.SetActive(false);
            __instance.enabled = false;
            return false;
        }
        __instance._thrusterArrowRoot.gameObject.SetActive(true);
        __instance.enabled = true;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipCockpitController), nameof(ShipCockpitController.ExitFlightConsole))]
    public static void UpdatePersistentInputAutopilotState()
    {
        if ((bool)enablePersistentInput.GetProperty())
        {
            Locator.GetShipBody().GetComponent<ShipPersistentInput>().UpdateLastAutopilotState();
        }
    }
    #endregion

    #region InputLatency
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ThrusterModel), nameof(ThrusterModel.AddTranslationalInput))]
    public static bool DelayTranslationalInput(ThrusterModel __instance, Vector3 input)
    {
        if (__instance is not ShipThrusterModel || (float)shipInputLatency.GetProperty() == 0f) return true;

        InputLatencyController.AddTranslationalInput(input);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ThrusterModel), nameof(ThrusterModel.AddRotationalInput))]
    public static bool DelayRotationalInput(ThrusterModel __instance, Vector3 input)
    {
        if (__instance is not ShipThrusterModel || (float)shipInputLatency.GetProperty() == 0f) return true;

        InputLatencyController.AddRotationalInput(input);
        return false;
    }

    #endregion

    #region EngineSwitch
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipResources), nameof(ShipResources.AreThrustersUsable))]
    public static void DisableThrustersWhenEngineOff(ref bool __result)
    {
        if ((bool)addEngineSwitch.GetProperty())
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
        if ((bool)shipIgnitionCancelFix.GetProperty())
        {
            ShipThrusterController thrustController = Locator.GetShipBody().GetComponent<ShipThrusterController>();
            if (thrustController && thrustController._isIgniting)
            {
                thrustController._isIgniting = false;
                GlobalMessenger.FireEvent("CancelShipIgnition");
            }
        }
    }
    #endregion
}