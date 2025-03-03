using UnityEngine;

namespace ShipEnhancements;

public class RadioCodeZone : MonoBehaviour
{
    [SerializeField]
    private AudioClip _audioCode;

    private OWTriggerVolume _triggerVolume;

    private void Start()
    {
        _triggerVolume = gameObject.GetAddComponent<OWTriggerVolume>();

        _triggerVolume.OnEntry += OnEffectVolumeEnter;
        _triggerVolume.OnExit += OnEffectVolumeExit;
    }

    private void OnEffectVolumeEnter(GameObject hitObj)
    {
        if (hitObj.TryGetComponent(out RadioCodeDetector detector))
        {
            detector.AddZone(this);
        }
    }

    private void OnEffectVolumeExit(GameObject hitObj)
    {
        if (hitObj.TryGetComponent(out RadioCodeDetector detector))
        {
            detector.RemoveZone(this);
        }
    }

    public AudioClip GetAudioCode()
    {
        return _audioCode;
    }

    private void OnDestroy()
    {
        if (_triggerVolume == null) return;
        _triggerVolume.OnEntry -= OnEffectVolumeEnter;
        _triggerVolume.OnExit -= OnEffectVolumeExit;
    }
}
