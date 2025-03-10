using UnityEngine;

namespace ShipEnhancements;

public class PlayerTorpidityEffect : MonoBehaviour
{
    private PlayerCameraEffectController _effectController;
    private OWRigidbody _shipBody;
    private float _minSpinSpeed;
    private float _maxSpinSpeed;
    private float _currentPercent;
    private float _effectSpeed = 0.2f;

    private void Awake()
    {
        _effectController = GetComponent<PlayerCameraEffectController>();
        _shipBody = SELocator.GetShipBody();

        GlobalMessenger<OWRigidbody>.AddListener("ShipCockpitDetached", OnCockpitDetached);
    }

    private void Start()
    {
        _minSpinSpeed = (ShipEnhancements.Instance.levelOneSpinSpeed * ShipEnhancements.Instance.levelOneSpinSpeed) / 2;
        _maxSpinSpeed = ShipEnhancements.Instance.maxSpinSpeed * ShipEnhancements.Instance.maxSpinSpeed;

        _effectController._owCamera.postProcessingSettings.vignette.color = Color.black;
        _effectController._owCamera.postProcessingSettings.vignette.rounded = true;
        _effectController._owCamera.postProcessingSettings.vignette.roundness = 1f;
        _effectController._owCamera.postProcessingSettings.vignette.opacity = 1f;
        _effectController._owCamera.postProcessingSettings.vignette.center = Vector3.one * 0.5f;
        _effectController._owCamera.postProcessingSettings.vignette.smoothness = 1f;
        _effectController._owCamera.postProcessingSettings.vignette.intensity = 0f;
        _effectController._owCamera.postProcessingSettings.vignetteEnabled = true;
    }

    private void Update()
    {
        float percent = 0f;
        if (PlayerState.AtFlightConsole())
        {
            percent = Mathf.InverseLerp(_minSpinSpeed, _maxSpinSpeed, SELocator.GetShipBody().GetAngularVelocity().sqrMagnitude);
        }
        if (!Mathf.Approximately(_currentPercent, percent))
        {
            float diff = percent - _currentPercent;
            float step;
            if (Mathf.Sign(diff) > 0)
            {
                step = Mathf.Min(Mathf.Abs(diff), Time.deltaTime * _effectSpeed);
            }
            else
            {
                step = Mathf.Min(Mathf.Abs(diff), Time.deltaTime * _effectSpeed * 0.1f);
            }
            _currentPercent += step * Mathf.Sign(diff);
            _effectController._owCamera.postProcessingSettings.vignette.intensity = _currentPercent;
        }
    }

    private void OnCockpitDetached(OWRigidbody body)
    {
        _shipBody = body;
    }

    private void OnDestroy()
    {
        GlobalMessenger<OWRigidbody>.RemoveListener("ShipCockpitDetached", OnCockpitDetached);
    }
}
