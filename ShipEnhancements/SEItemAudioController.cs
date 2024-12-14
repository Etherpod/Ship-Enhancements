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

    public static void Initialize()
    {
        _tractorBeamDrop = ShipEnhancements.LoadAudio(AudioClipPath + "PutDown_BigRock_01.ogg");
        _tractorBeamPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "Pickup_BigRock_01.ogg");
        _flagDrop = ShipEnhancements.LoadAudio(AudioClipPath + "ExpeditionFlag_PutDown.ogg");
        _flagPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "ExpeditionFlag_PickUp.ogg");
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
        else if (itemType == ShipEnhancements.Instance.ExpeditionFlagItemType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_flagDrop, DefaultVolume);
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
        else if (itemType == ShipEnhancements.Instance.ExpeditionFlagItemType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_flagPickUp, DefaultVolume);
        }
    }
}
