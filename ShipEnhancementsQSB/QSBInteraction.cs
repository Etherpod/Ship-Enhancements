using UnityEngine;
using QSB.ShipSync;
using QSB.ShipSync.TransformSync;
using QSB.Player;
using HarmonyLib;
using QSB.RespawnSync;
using ShipEnhancements;
using System.Reflection;
using QSB.WorldSync;
using QSB.ShipSync.WorldObjects;
using QSB.Messaging;
using QSB.ShipSync.Messages.Hull;
using QSB.ItemSync.Messages;
using System;
using QSB.ItemSync.WorldObjects;
using QSB.SectorSync.WorldObjects;
using QSB.ItemSync;
using QSB.ItemSync.WorldObjects.Items;
using QSB.ShipSync.Patches;
using static ShipEnhancements.ShipEnhancements.Settings;
using QSB.Tools.ProbeLauncherTool.Patches;
using QSB.ShipSync.Messages;
using QSB.Player.TransformSync;
using QSB;

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

            ShipEnhancements.ShipEnhancements.Instance.ModHelper.Events.Unity.FireInNUpdates(() =>
            {
                if ((bool)addPortableCampfire.GetProperty())
                {
                    QSBWorldSync.Init<QSBPortableCampfireItem, PortableCampfireItem>();
                }
                if ((bool)addPortableTractorBeam.GetProperty())
                {
                    QSBWorldSync.Init<QSBPortableTractorBeamItem, PortableTractorBeamItem>();
                }
                if ((bool)addTether.GetProperty())
                {
                    QSBWorldSync.Init<QSBTetherHookItem, TetherHookItem>();
                }
                if ((bool)addExpeditionFlag.GetProperty())
                {
                    QSBWorldSync.Init<QSBExpeditionFlagItem, ExpeditionFlagItem>();
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

    public void SetHullDamaged(ShipHull shipHull)
    {
        var hull = shipHull.GetWorldObject<QSBShipHull>();
        hull.SendMessage(new HullDamagedMessage());
        hull.SendMessage(new HullChangeIntegrityMessage(shipHull._integrity));
    }

    public int GetIDFromTetherHook(TetherHookItem hookItem)
    {
        var worldObj = hookItem.GetWorldObject<QSBTetherHookItem>();
        return worldObj.ObjectId;
    }

    public TetherHookItem GetTetherHookFromID(int hookID)
    {
        return hookID.GetWorldObject<QSBTetherHookItem>().AttachedObject;
    }

    public bool WorldObjectsLoaded()
    {
        return QSBWorldSync.AllObjectsReady;
    }

    public void OnDetachAllPlayers(Vector3 velocity)
    {
        QSBInteractionPatches.NextImpactVelocity = velocity;
    }

    public void UpdateShipThrusterSync()
    {
        if (!QSBPlayerManager.LocalPlayer.FlyingShip)
        {
            var sync = ShipTransformSync.LocalInstance.ThrusterVariableSyncer.AccelerationSyncer;
            //ShipEnhancements.ShipEnhancements.WriteDebugMessage(sync.Value);
            sync.Value = SELocator.GetShipBody().GetComponent<ShipThrusterModel>().GetLocalAcceleration();
        }
    }
}

[HarmonyPatch]
public static class QSBInteractionPatches
{
    #region Ship Refuel Drain
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
    #endregion

    #region Ship Item Placement
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DropItemMessage), "ProcessInputs")]
    public static bool CheckForShipBodyDrop(
        Vector3 worldPosition,
        Vector3 worldNormal,
        Transform parent,
        Sector sector,
        IItemDropTarget customDropTarget,
        OWRigidbody targetRigidbody,
        ref (Vector3 localPosition, Vector3 localNormal, int sectorId, int dropTargetId, int rigidBodyId) __result)
    {
        (Vector3 localPosition, Vector3 localNormal, int sectorId, int dropTargetId, int rigidBodyId) tuple = new();

        if (customDropTarget == null)
        {
            if (targetRigidbody is ShipBody)
            {
                tuple.rigidBodyId = -2;
            }
            else
            {
                tuple.rigidBodyId = targetRigidbody.GetWorldObject<QSBOWRigidbody>().ObjectId;
            }
            tuple.dropTargetId = -1;
        }
        else
        {
            tuple.rigidBodyId = -1;
            tuple.dropTargetId = ((MonoBehaviour)customDropTarget).GetWorldObject<IQSBDropTarget>().ObjectId;
        }

        tuple.sectorId = sector ? sector.GetWorldObject<QSBSector>().ObjectId : -1;
        tuple.localPosition = parent.InverseTransformPoint(worldPosition);
        tuple.localNormal = parent.InverseTransformDirection(worldNormal);

        __result = tuple;

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DropItemMessage), nameof(DropItemMessage.OnReceiveRemote))]
    public static bool AllowShipItemDrop(DropItemMessage __instance)
    {
        (Vector3 localPosition, Vector3 localNormal, int sectorId, int dropTargetId, int rigidBodyId) Data
            = ((Vector3, Vector3, int, int, int))typeof(DropItemMessage).GetProperty("Data", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).GetValue(__instance);

        IQSBItem WorldObject = (IQSBItem)typeof(DropItemMessage).GetProperty("WorldObject",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(__instance);

        var customDropTarget = Data.dropTargetId == -1
            ? null
            : Data.dropTargetId.GetWorldObject<IQSBDropTarget>().AttachedObject;

        /*var parent = customDropTarget == null
            ? Data.rigidBodyId.GetWorldObject<QSBOWRigidbody>().AttachedObject.transform
            : customDropTarget.GetItemDropTargetTransform(null);*/

        Transform parent;

        if (customDropTarget == null)
        {
            if (Data.rigidBodyId == -2)
            {
                parent = Locator.GetShipBody().transform;
            }
            else
            {
                parent = Data.rigidBodyId.GetWorldObject<QSBOWRigidbody>().AttachedObject.transform;
            }
        }
        else
        {
            parent = customDropTarget.GetItemDropTargetTransform(null);
        }

        var worldPos = parent.TransformPoint(Data.localPosition);
        var worldNormal = parent.TransformDirection(Data.localNormal);

        var sector = Data.sectorId != -1 ? Data.sectorId.GetWorldObject<QSBSector>().AttachedObject : null;

        WorldObject.DropItem(worldPos, worldNormal, parent, sector, customDropTarget);
        WorldObject.ItemState.HasBeenInteractedWith = true;
        WorldObject.ItemState.State = ItemStateType.OnGround;
        WorldObject.ItemState.LocalPosition = Data.localPosition;
        WorldObject.ItemState.Parent = parent;
        WorldObject.ItemState.LocalNormal = Data.localNormal;
        WorldObject.ItemState.Sector = sector;
        WorldObject.ItemState.CustomDropTarget = customDropTarget;
        WorldObject.ItemState.Rigidbody = parent.GetComponent<OWRigidbody>();

        var player = QSBPlayerManager.GetPlayer(__instance.From);
        player.HeldItem = null;
        player.AnimationSync.VisibleAnimator.SetTrigger("DropHeldItem");

        return false;
    }
    #endregion

    #region Ship Damage Multiplier
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipPatches), nameof(ShipPatches.ShipHull_FixedUpdate))]
    public static bool ApplyHullDamageMultiplier(ShipHull __0)
    {
        if ((float)shipDamageMultiplier.GetProperty() == 1 && (float)shipDamageSpeedMultiplier.GetProperty() == 1)
        {
            return true;
        }

        if (__0._dominantImpact != null)
        {
            var damage = Mathf.InverseLerp(30f * (float)shipDamageSpeedMultiplier.GetProperty(),
                200f * (float)shipDamageSpeedMultiplier.GetProperty(), __0._dominantImpact.speed);
            if (damage > 0f)
            {
                var num2 = 0.15f;
                if (damage < num2 && __0._integrity > 1f - num2)
                {
                    damage = num2;
                }

                damage *= (float)shipDamageMultiplier.GetProperty();

                __0._integrity = Mathf.Max(__0._integrity - damage, 0f);
                var qsbShipHull = __0.GetWorldObject<QSBShipHull>();
                if (!__0._damaged)
                {
                    __0._damaged = true;

                    //__instance.RaiseEvent(nameof(__instance.OnDamaged), __instance);

                    var eventDelegate1 = (MulticastDelegate)typeof(ShipHull).GetField("OnDamaged", BindingFlags.Instance 
                        | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__0);
                    if (eventDelegate1 != null)
                    {
                        foreach (var handler in eventDelegate1.GetInvocationList())
                        {
                            handler.Method.Invoke(handler.Target, [__0]);
                        }
                    }

                    qsbShipHull
                        .SendMessage(new HullDamagedMessage());
                }

                if (__0._damageEffect != null)
                {
                    __0._damageEffect.SetEffectBlend(1f - __0._integrity);
                }

                qsbShipHull
                    .SendMessage(new HullChangeIntegrityMessage(__0._integrity));
            }

            foreach (var component in __0._components)
            {
                if (!(component == null) && !component.isDamaged)
                {
                    if (component.ApplyImpact(__0._dominantImpact))
                    {
                        break;
                    }
                }
            }

            //__instance.RaiseEvent(nameof(__instance.OnImpact), __instance._dominantImpact, damage);

            var eventDelegate2 = (MulticastDelegate)typeof(ShipHull).GetField("OnImpact", BindingFlags.Instance 
                | BindingFlags.NonPublic | BindingFlags.Public).GetValue(__0);
            if (eventDelegate2 != null)
            {
                foreach (var handler in eventDelegate2.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, [__0._dominantImpact, damage]);
                }
            }

            __0._dominantImpact = null;
        }

        __0.enabled = false;
        return false;
    }
    #endregion

    #region Disable Map Markers
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerMapMarker), "ShouldBeVisible")]
    public static void DisablePlayerMapMarker(ref bool __result)
    {
        if ((bool)disableMapMarkers.GetProperty())
        {
            __result = false;
        }
    }
    #endregion

    #region Scout
    [HarmonyPrefix]
    [HarmonyPatch(typeof(LauncherPatches), nameof(LauncherPatches.ProbeLauncher_RetrieveProbe))]
    public static bool CancelRetrieveProbe(ProbeLauncher __0, bool forcedRetrieval)
    {
        return PatchClass.CanRetrieveProbe(__0, forcedRetrieval);
    }
    #endregion

    #region DisableSeatbelt
    public static Vector3 NextImpactVelocity;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ShipCustomAttach), "Update")]
    public static void UnbuckleAllPlayers(PlayerAttachPoint ____playerAttachPoint)
    {
        if (NextImpactVelocity != Vector3.zero)
        {
            if (____playerAttachPoint.enabled)
            {
                ____playerAttachPoint.DetachPlayer();
                SELocator.GetPlayerBody().AddForce(NextImpactVelocity);
            }

            NextImpactVelocity = Vector3.zero;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FlyShipMessage), nameof(FlyShipMessage.OnReceiveRemote))]
    public static bool RemoveBuckleAudio(FlyShipMessage __instance)
    {
        if (!(bool)disableSeatbelt.GetProperty())
        {
            return true;
        }

        //__instance.SetCurrentFlyer(__instance.From, __instance.Data);

        typeof(FlyShipMessage).GetMethod("SetCurrentFlyer", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Invoke(__instance, [__instance.From, __instance.Data]);

        var shipCockpitController = ShipManager.Instance.CockpitController;

        if (shipCockpitController == null)
        {
            return false;
        }

        if (__instance.Data)
        {
            //QSBPlayerManager.GetPlayer(__instance.From)?.AudioController?.PlayOneShot(AudioType.ShipCockpitBuckleUp);
            shipCockpitController._interactVolume?.DisableInteraction();
        }
        else
        {
            //QSBPlayerManager.GetPlayer(__instance.From)?.AudioController?.PlayOneShot(AudioType.ShipCockpitUnbuckle);
            shipCockpitController._interactVolume?.EnableInteraction();
        }

        return false;
    }
    #endregion

    #region InputLatency
    public static bool test = false;
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipThrusterVariableSyncer), nameof(ShipThrusterVariableSyncer.Update))]
    public static bool KeepReadingInputLatency(ShipThrusterVariableSyncer __instance, ShipThrusterModel ____thrusterModel, ShipThrusterAudio ____thrusterAudio)
    {
        if ((float)shipInputLatency.GetProperty() == 0f)
        {
            return true;
        }

        /*if (InputLatencyController.IsInputQueued != test)
        {
            test = InputLatencyController.IsInputQueued;
            ShipEnhancements.ShipEnhancements.WriteDebugMessage(QSBCore.IsHost + " Input: " + test);
        }*/

        // bug : this doesn't account for autopilot
        // fixes #590
        if (ShipManager.Instance.CurrentFlyer == uint.MaxValue)
        {
            if (____thrusterModel && !InputLatencyController.IsInputQueued)
            {
                __instance.AccelerationSyncer.Value = Vector3.zero;
            }

            return false;
        }
        else
        {
            test = false;
        }

        if (PlayerTransformSync.LocalInstance && (QSBPlayerManager.LocalPlayer.FlyingShip || InputLatencyController.IsInputQueued))
        {
            ShipEnhancements.ShipEnhancements.QSBInteraction.UpdateShipThrusterSync();
            return false;
        }

        if (__instance.AccelerationSyncer.public_HasChanged())
        {
            if (__instance.AccelerationSyncer.Value == Vector3.zero)
            {
                foreach (var item in ShipThrusterManager.ShipFlameControllers)
                {
                    item.OnStopTranslationalThrust();
                }

                ____thrusterAudio.OnStopTranslationalThrust();

                ShipThrusterManager.ShipWashController.OnStopTranslationalThrust();
            }
            else
            {
                foreach (var item in ShipThrusterManager.ShipFlameControllers)
                {
                    item.OnStartTranslationalThrust();
                }

                ____thrusterAudio.OnStartTranslationalThrust();

                ShipThrusterManager.ShipWashController.OnStartTranslationalThrust();
            }
        }

        return false;
    }
    #endregion
}