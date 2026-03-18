using UnityEngine;

namespace ShipEnhancements;

public class SatelliteAchievement : MonoBehaviour
{
    private OWTriggerVolume _triggerVolume;

    private void Awake()
    {
        _triggerVolume = GetComponent<OWTriggerVolume>();
        _triggerVolume.OnEntry += OnEntry;
    }

    private void OnEntry(GameObject hitObj)
    {
        ShipEnhancements.WriteDebugMessage("On entry " + hitObj.name);
        if (ShipEnhancements.AchievementsAPI != null
            && !SEAchievementTracker.Satellite 
            && hitObj.name == "Detector_HearthianMapSatellite")
        {
            SEAchievementTracker.Satellite = true;
            ShipEnhancements.AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.SATELLITE");
        }
    }

    private void OnDestroy()
    {
        _triggerVolume.OnEntry -= OnEntry;
    }
}
