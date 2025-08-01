using UnityEngine;
using HarmonyLib;

[HarmonyPatch]
public static class InheritableForcesFix
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ForceDetector), nameof(ForceDetector.GetForceAcceleration))]
    public static bool InheritableForcesFix_GetForceAcceleration_Direct(ForceDetector __instance, ref Vector3 __result)
    {
        if (__instance is not DynamicForceDetector) return true;

        if (__instance._dirty)
        {
            InheritableForcesFix_AccumulateAcceleration(__instance, false);
            __instance._dirty = false;
        }
        __result = __instance._netAcceleration;
        return false;
    }

    public static Vector3 InheritableForcesFix_GetForceAcceleration_Remote(ForceDetector __instance, bool fromInheritable)
    {
        if (__instance._dirty)
        {
            InheritableForcesFix_AccumulateAcceleration(__instance, fromInheritable);
            __instance._dirty = false;
        }
        return __instance._netAcceleration;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ForceDetector), nameof(ForceDetector.ManagedFixedUpdate))]
    public static bool InheritableForcesFix_ManagedFixedUpdate(ForceDetector __instance)
    {
        if (__instance is not DynamicForceDetector) return true;

        if (__instance._dirty)
        {
            InheritableForcesFix_AccumulateAcceleration(__instance, false);
            __instance._dirty = false;
        }
        return false;
    }

    public static void InheritableForcesFix_AccumulateAcceleration(ForceDetector __instance, bool fromInheritable)
    {
        if (__instance is AlignmentForceDetector)
        {
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
            if (detector._activeInheritedDetector != null && !fromInheritable)
            {
                detector._netAcceleration += InheritableForcesFix_GetForceAcceleration_Remote(detector._activeInheritedDetector, true);
            }

            return;
        }

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
        if (__instance._activeInheritedDetector != null && !fromInheritable)
        {
            __instance._netAcceleration += InheritableForcesFix_GetForceAcceleration_Remote(__instance._activeInheritedDetector, true);
        }
    }
}
