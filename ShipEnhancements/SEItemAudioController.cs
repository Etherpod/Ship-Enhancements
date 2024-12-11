﻿using HarmonyLib;
using UnityEngine;

namespace ShipEnhancements;

[HarmonyPatch]
public static class SEItemAudioController
{
    private static readonly string AudioClipPath = "Assets/ShipEnhancements/AudioClip/";
    private static readonly float DefaultVolume = 0.5f;

    private static AudioClip _tractorBeamDrop;
    private static AudioClip _tractorBeamPickUp;

    public static void Initialize()
    {
        _tractorBeamDrop = ShipEnhancements.LoadAudio(AudioClipPath + "PutDown_BigRock_01.ogg");
        _tractorBeamPickUp = ShipEnhancements.LoadAudio(AudioClipPath + "Pickup_BigRock_01.ogg");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAudioController), nameof(PlayerAudioController.PlayInsertItem))]
    public static void AddInsertItemAudio(PlayerAudioController __instance, ItemType itemType, bool __runOriginal)
    {
        if (!__runOriginal)
        {
            return;
        }

        if (itemType == ShipEnhancements.Instance.portableTractorBeamType)
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

        if (itemType == ShipEnhancements.Instance.portableTractorBeamType)
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

        if (itemType == ShipEnhancements.Instance.portableTractorBeamType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_tractorBeamDrop, DefaultVolume);
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

        if (itemType == ShipEnhancements.Instance.portableTractorBeamType)
        {
            __instance._oneShotExternalSource.PlayOneShot(_tractorBeamPickUp, DefaultVolume);
        }
    }
}
