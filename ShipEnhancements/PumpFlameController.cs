using UnityEngine;

namespace ShipEnhancements;

public class PumpFlameController : MonoBehaviour
{
    [SerializeField]
    private Light _light;
    [SerializeField]
    private DampedSpring _scaleSpring = new DampedSpring();

    private MeshRenderer _thrusterRenderer;
    private HazardVolume _hazardVolume;
    private bool _thrustersFiring;
    private float _baseLightRadius;
    private float _currentScale;

    private void Awake()
    {
        _thrusterRenderer = GetComponent<MeshRenderer>();
        if ((bool)ShipEnhancements.Settings.hotThrusters.GetProperty())
        {
            _hazardVolume = GetComponentInChildren<HazardVolume>();
            _hazardVolume.gameObject.SetActive(false);
        }
        else
        {
            GetComponentInChildren<HazardVolume>().gameObject.SetActive(false);
        }

        _thrustersFiring = false;
        _baseLightRadius = _light.range;
        _currentScale = 0f;
        _thrusterRenderer.enabled = false;
        _light.enabled = false;
        enabled = false;
    }

    private void Update()
    {
        float num = _thrustersFiring ? 1f : 0f;
        _currentScale = _scaleSpring.Update(_currentScale, num, Time.deltaTime);
        if (_currentScale < 0f)
        {
            _currentScale = 0f;
            _scaleSpring.ResetVelocity();
        }
        if (!_thrustersFiring && _currentScale <= 0.001f)
        {
            _currentScale = 0f;
            _scaleSpring.ResetVelocity();
            enabled = false;
        }
        transform.localScale = Vector3.one * _currentScale;
        _light.range = _baseLightRadius * _currentScale;
        _thrusterRenderer.enabled = _currentScale > 0f;
        _light.enabled = _currentScale > 0f;
        _hazardVolume?.gameObject.SetActive(_currentScale > 0f);
    }

    public void ActivateFlame()
    {
        _thrustersFiring = true;
        enabled = true;
    }

    public void DeactivateFlame()
    {
        _thrustersFiring = false;
    }
}
