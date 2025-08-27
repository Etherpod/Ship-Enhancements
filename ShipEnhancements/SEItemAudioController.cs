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
    private static AudioClip _tetherHookPickUp;
    private static AudioClip _tetherHookSocket;
    private static AudioClip _tetherHookUnsocket;
    private static AudioClip _radioDrop;
    private static AudioClip _radioPickUp;
    private static AudioClip _portableCampfireDrop;
    private static AudioClip _portableCampfirePickUp;
    private static AudioClip _portableCampfireSocket;
    private static AudioClip _resourcePumpDrop;
    private static AudioClip _resourcePumpPickUp;

    public static void Initialize()
    {
        _tractorBeamDrop = ShipEnhancements.LoadAudio(AudioClipPath + "PutDown_BigRock_01.ogg");
        _tractorBeamPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "Pickup_BigRock_01.ogg");
        _flagDrop = ShipEnhancements.LoadAudio(AudioClipPath + "ExpeditionFlag_Drop.ogg");
        _flagPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "ExpeditionFlag_PickUp.ogg");
        _fuelTankDrop = ShipEnhancements.LoadAudio(AudioClipPath + "FuelCanister_Drop.ogg");
        _fuelTankPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "FuelCanister_PickUp.ogg");
        _gravityCrystalDrop = ShipEnhancements.LoadAudio(AudioClipPath + "GravityCrystal_Drop.ogg");
        _gravityCrystalPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "GravityCrystal_PickUp.ogg");
        _repairWrenchDrop = ShipEnhancements.LoadAudio(AudioClipPath + "RepairWrench_Drop.ogg");
        _repairWrenchPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "RepairWrench_PickUp.ogg");
        _tetherHookDrop = ShipEnhancements.LoadAudio(AudioClipPath + "TetherHook_Drop.mp3");
        _tetherHookPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "TetherHook_PickUp.ogg");
        _tetherHookSocket = ShipEnhancements.LoadAudio(AudioClipPath + "TetherHook_Socket.ogg");
        _tetherHookUnsocket = ShipEnhancements.LoadAudio(AudioClipPath + "TetherHook_Unsocket.ogg");
        _radioDrop = ShipEnhancements.LoadAudio(AudioClipPath + "Radio_Drop.ogg");
        _radioPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "Radio_PickUp.ogg");
        _portableCampfireDrop = ShipEnhancements.LoadAudio(AudioClipPath + "PortableCampfire_Unpack.ogg");
        _portableCampfirePickUp = ShipEnhancements.LoadAudio(AudioClipPath + "PortableCampfire_PickUpLogs.ogg");
        _portableCampfireSocket = ShipEnhancements.LoadAudio(AudioClipPath + "PortableCampfire_DropLogs.ogg");
        _resourcePumpDrop = ShipEnhancements.LoadAudio(AudioClipPath + "Pump_Drop.ogg");
        _resourcePumpPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "Pump_PickUp.ogg");
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
        else if (itemType == ShipEnhancements.Instance.ExpeditionFlagType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_flagDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.FuelTankType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_fuelTankDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.GravityCrystalType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_gravityCrystalDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.RepairWrenchType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_repairWrenchDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.TetherHookType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_tetherHookSocket, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.RadioType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_radioDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.PortableCampfireType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_portableCampfireSocket, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.ResourcePumpType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_resourcePumpDrop, DefaultVolume);
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
        else if (itemType == ShipEnhancements.Instance.ExpeditionFlagType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_flagPickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.FuelTankType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_fuelTankPickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.GravityCrystalType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_gravityCrystalPickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.RepairWrenchType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_repairWrenchPickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.TetherHookType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_tetherHookUnsocket, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.RadioType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_radioPickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.PortableCampfireType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_portableCampfirePickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.ResourcePumpType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_resourcePumpPickUp, DefaultVolume);
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
        else if (itemType == ShipEnhancements.Instance.FuelTankType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_fuelTankDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.GravityCrystalType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_gravityCrystalDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.RepairWrenchType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_repairWrenchDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.TetherHookType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_tetherHookDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.RadioType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_radioDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.PortableCampfireType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_portableCampfireDrop, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.ResourcePumpType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_resourcePumpDrop, DefaultVolume);
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
        else if (itemType == ShipEnhancements.Instance.FuelTankType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_fuelTankPickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.GravityCrystalType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_gravityCrystalPickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.RepairWrenchType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_repairWrenchPickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.TetherHookType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_tetherHookPickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.RadioType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_radioPickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.PortableCampfireType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_portableCampfirePickUp, DefaultVolume);
        }
        else if (itemType == ShipEnhancements.Instance.ResourcePumpType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_resourcePumpPickUp, DefaultVolume);
        }
    }
}
