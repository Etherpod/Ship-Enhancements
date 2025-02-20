using HarmonyLib;
using UnityEngine;

namespace ShipEnhancements;

[HarmonyPatch]
public static class SEItemAudioController
{
    private static readonly string AudioClipPath = "Assets/ShipEnhancements/AudioClip/";
    private static readonly float DefaultVolume = 0.5f;

    private static AudioClip _tractorBeamDrop;
    private static AudioClip _tractorBeamPickUp;
    private static AudioClip _flagDrop;
    private static AudioClip _flagPickUp;
    private static AudioClip _fuelTankDrop;
    private static AudioClip _fuelTankPickUp;
    private static AudioClip _gravityCrystalDrop;
    private static AudioClip _gravityCrystalPickUp;
    private static AudioClip _repairWrenchDrop;
    private static AudioClip _repairWrenchPickUp;
    private static AudioClip _tetherHookDrop;

    public static void Initialize()
    {
        _tractorBeamDrop = ShipEnhancements.LoadAudio(AudioClipPath + "PutDown_BigRock_01.ogg");
        _tractorBeamPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "Pickup_BigRock_01.ogg");
        _flagDrop = ShipEnhancements.LoadAudio(AudioClipPath + "ExpeditionFlag_PutDown.ogg");
        _flagPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "ExpeditionFlag_PickUp.ogg");
        _fuelTankDrop = ShipEnhancements.LoadAudio(AudioClipPath + "FuelCanister_Drop.ogg");
        _fuelTankPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "FuelCanister_PickUp.ogg");
        _gravityCrystalDrop = ShipEnhancements.LoadAudio(AudioClipPath + "GravityCrystal_Drop.ogg");
        _gravityCrystalPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "GravityCrystal_PickUp.ogg");
        _repairWrenchDrop = ShipEnhancements.LoadAudio(AudioClipPath + "RepairWrench_Drop.ogg");
        _repairWrenchPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "RepairWrench_PickUp.ogg");
        _tetherHookDrop = ShipEnhancements.LoadAudio(AudioClipPath + "TetherHook_Drop.ogg");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAudioController), nameof(PlayerAudioController.PlayInsertItem))]
    public static void AddInsertItemAudio(PlayerAudioController __instance, ItemType itemType, bool __runOriginal)
    {
        if (!__runOriginal)
        {
            return;
        }

        if (itemType == ShipEnhancements.Instance.PortableTractorBeamType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_tractorBeamDrop, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.FuelTankType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_fuelTankDrop, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.GravityCrystalType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_gravityCrystalDrop, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.RepairWrenchType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_repairWrenchDrop, DefaultVolume);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAudioController), nameof(PlayerAudioController.PlayRemoveItem))]
    public static void AddRemoveItemAudio(PlayerAudioController __instance, ItemType itemType, bool __runOriginal)
    {
        if (!__runOriginal)
        {
            return;
        }

        if (itemType == ShipEnhancements.Instance.PortableTractorBeamType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_tractorBeamPickUp, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.FuelTankType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_fuelTankPickUp, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.GravityCrystalType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_gravityCrystalPickUp, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.RepairWrenchType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_repairWrenchPickUp, DefaultVolume);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAudioController), nameof(PlayerAudioController.PlayDropItem))]
    public static void AddDropItemAudio(PlayerAudioController __instance, ItemType itemType, bool __runOriginal)
    {
        if (!__runOriginal)
        {
            return;
        }

        if (itemType == ShipEnhancements.Instance.PortableTractorBeamType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_tractorBeamDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.ExpeditionFlagType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_flagDrop, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.FuelTankType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_fuelTankDrop, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.GravityCrystalType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_gravityCrystalDrop, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.RepairWrenchType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_repairWrenchDrop, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.TetherHookType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_tetherHookDrop, DefaultVolume);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAudioController), nameof(PlayerAudioController.PlayPickUpItem))]
    public static void AddPickUpItemAudio(PlayerAudioController __instance, ItemType itemType, bool __runOriginal)
    {
        if (!__runOriginal)
        {
            return;
        }

        if (itemType == ShipEnhancements.Instance.PortableTractorBeamType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_tractorBeamPickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.ExpeditionFlagType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_flagPickUp, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.FuelTankType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_fuelTankPickUp, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.GravityCrystalType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_gravityCrystalPickUp, DefaultVolume);
        }
        if (itemType == ShipEnhancements.Instance.RepairWrenchType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_repairWrenchPickUp, DefaultVolume);
        }
    }
}
