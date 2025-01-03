using UnityEngine;

namespace ShipEnhancements;

public class ShipAudioSignal : AudioSignal
{
    FogWarpDetector _shipWarpDetector;

    public override void Awake()
    {
        base.Awake();
        _shipWarpDetector = SELocator.GetShipDetector().GetComponent<FogWarpDetector>();
        _shipWarpDetector.OnOuterFogWarpVolumeChange += OnOuterFogWarpVolumeChange;
        SetSector(SELocator.GetShipSector());
        _name = ShipEnhancements.Instance.ShipSignalName;
        _frequency = SignalFrequency.Traveler;
    }

    private void OnOuterFogWarpVolumeChange(OuterFogWarpVolume warpVolume)
    {
        _outerFogWarpVolume = warpVolume;
    }

    public void UpdateShipSignalStrength(Signalscope scope, float distToClosestScopeObstruction)
    {
        _canBePickedUpByScope = false;
        if (_sunController != null && _sunController.IsPointInsideSupernova(transform.position))
        {
            _signalStrength = 0f;
            _degreesFromScope = 180f;
            return;
        }
        if (Locator.GetQuantumMoon() != null && Locator.GetQuantumMoon().IsPlayerInside() && _name != SignalName.Quantum_QM)
        {
            _signalStrength = 0f;
            _degreesFromScope = 180f;
            return;
        }
        if (SELocator.GetShipDamageController() != null && SELocator.GetShipDamageController().IsSystemFailed())
        {
            _signalStrength = 0f;
            _degreesFromScope = 180f;
            return;
        }
        if (SELocator.GetSignalscopeComponent() != null && SELocator.GetSignalscopeComponent().isDamaged)
        {
            _signalStrength = 0f;
            _degreesFromScope = 180f;
            return;
        }
        if (!_active || !gameObject.activeInHierarchy || PlayerState.IsInsideShip() || OWInput.IsInputMode(InputMode.ShipCockpit) 
            || _outerFogWarpVolume != PlayerState.GetOuterFogWarpVolume() 
            || (scope.GetFrequencyFilter() & _frequency) != _frequency)
        {
            _signalStrength = 0f;
            _degreesFromScope = 180f;
            return;
        }
        _scopeToSignal = transform.position - scope.transform.position;
        _distToScope = _scopeToSignal.magnitude;
        if (_outerFogWarpVolume == null && distToClosestScopeObstruction < 1000f && _distToScope > 1000f)
        {
            _signalStrength = 0f;
            _degreesFromScope = 180f;
            return;
        }
        _canBePickedUpByScope = true;
        if (_distToScope < _sourceRadius)
        {
            _signalStrength = 1f;
        }
        else
        {
            _degreesFromScope = Vector3.Angle(scope.GetScopeDirection(), _scopeToSignal);
            float num = Mathf.InverseLerp(2000f, 1000f, _distToScope);
            float num2 = Mathf.Lerp(45f, 90f, num);
            float num3 = 57.29578f * Mathf.Atan2(_sourceRadius, _distToScope);
            float num4 = Mathf.Lerp(Mathf.Max(num3, 5f), Mathf.Max(num3, 1f), scope.GetZoomFraction());
            _signalStrength = Mathf.Clamp01(Mathf.InverseLerp(num2, num4, _degreesFromScope));
        }
        if (Locator.GetCloakFieldController() != null)
        {
            float num5;

            if (!Locator.GetCloakFieldController().isShipInsideCloak)
            {
                num5 = 1f - Locator.GetCloakFieldController().playerCloakFactor;
            }
            else
            {
                num5 = Locator.GetCloakFieldController().playerCloakFactor;
            }

            _signalStrength *= num5;
            if (OWMath.ApproxEquals(num5, 0f, 0.001f))
            {
                _signalStrength = 0f;
                _degreesFromScope = 180f;
                return;
            }
        }
        if (_distToScope < _identificationDistance + _sourceRadius && _signalStrength > 0.9f)
        {
            if (!PlayerData.KnowsFrequency(_frequency) && !_preventIdentification)
            {
                IdentifyFrequency();
            }
            if (!PlayerData.KnowsSignal(_name) && !_preventIdentification)
            {
                IdentifySignal();
            }
            if (_revealFactID.Length > 0)
            {
                Locator.GetShipLogManager().RevealFact(_revealFactID, true, true);
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _shipWarpDetector.OnOuterFogWarpVolumeChange -= OnOuterFogWarpVolumeChange;
    }
}
