using System;
using UnityEngine;

namespace ShipEnhancements;

public class ThrustModulatorButton : MonoBehaviour
{
    [SerializeField]
    private int _modulatorLevel;

    private InteractReceiver _interactReceiver;
    private ThrustModulatorController _modulatorController;
    private OWEmissiveRenderer _emissiveRenderer;
    private bool _active;
    private float _fadeTime = 0.1f;
    private float _fadeStartTime;
    private bool _fading = false;
    private float _lastEmissiveScale = 0f;

    private void Start()
    {
        _interactReceiver = GetComponent<InteractReceiver>();
        _modulatorController = GetComponentInParent<ThrustModulatorController>();
        _emissiveRenderer = GetComponent<OWEmissiveRenderer>();
        _interactReceiver._screenPrompt._text = "Set Thrust Modulator";
        _interactReceiver.OnPressInteract += OnPressInteract;

        SetButtonLight(true);
    }

    private void Update()
    {
        if (_fading)
        {
            float num = Mathf.InverseLerp(_fadeStartTime, _fadeStartTime + _fadeTime, Time.time);
            if (_active)
            {
                _emissiveRenderer.SetEmissiveScale(Mathf.Lerp(_lastEmissiveScale, 1f, num));
            }
            else
            {
                _emissiveRenderer.SetEmissiveScale(Mathf.Lerp(_lastEmissiveScale, 0f, num));
            }

            if (num >= 1)
            {
                _fading = false;
            }
        }
    }

    private void OnPressInteract()
    {
        ShipEnhancements.Instance.SetThrustModulatorLevel(_modulatorLevel);
        _modulatorController.UpdateModulatorDisplay(_modulatorLevel);
    }

    public int GetModulatorLevel()
    {
        return _modulatorLevel;
    }

    public void SetButtonLight(bool state)
    {
        _active = state;
        _fadeStartTime = Time.time;
        _lastEmissiveScale = _emissiveRenderer.GetEmissiveScale();
        _fading = true;
    }
}
