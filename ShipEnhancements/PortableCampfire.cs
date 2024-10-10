using UnityEngine;

namespace ShipEnhancements;

public class PortableCampfire : Campfire
{
    [SerializeField]
    private MeshCollider _collider;
    [SerializeField]
    private FluidDetector _fluidDetector;
    [SerializeField]
    private AudioClip _waterExtinguishAudio;

    private ScreenPrompt _cancelPrompt;
    private bool _extinguished = true;
    private string _packUpText = "Pack up";
    private string _extinguishText = "Extinguish";
    private bool _insideShip;
    private float _reactorHeatMeter;
    private float _reactorHeatMeterLength;
    private bool _shipDestroyed;
    private bool _lastOutsideWaterState = false;

    public override void Awake()
    {
        base.Awake();

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _cancelPrompt = new ScreenPrompt(InputLibrary.cancel, "Pack up", 0, ScreenPrompt.DisplayState.Normal, false);
        _reactorHeatMeter = 0f;
        _reactorHeatMeterLength = Random.Range(10f, 30f);
    }

    public void UpdateCampfire()
    {
        if (!_shipDestroyed && _insideShip && !_extinguished)
        {
            float oxygenDrain = 10f * Time.deltaTime;
            SELocator.GetShipResources().DrainOxygen(oxygenDrain);

            _reactorHeatMeter += Time.deltaTime;
            if (_reactorHeatMeter >= _reactorHeatMeterLength)
            {
                _reactorHeatMeter = 0f;
                _reactorHeatMeterLength = Random.Range(10f, 30f);
                SELocator.GetShipBody().GetComponentInChildren<ShipReactorComponent>().SetDamaged(true);

                if (!SEAchievementTracker.FireHazard && ShipEnhancements.AchievementsAPI != null)
                {
                    SEAchievementTracker.FireHazard = true;
                    ShipEnhancements.AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.FIRE_HAZARD");
                }
            }
        }
        if (_extinguished)
        {
            bool outsideWater = IsOutsideWater();
            if (_lastOutsideWaterState != outsideWater)
            {
                _lastOutsideWaterState = outsideWater;
                _interactVolume._screenPrompt.SetDisplayState(outsideWater ? ScreenPrompt.DisplayState.Normal : ScreenPrompt.DisplayState.GrayedOut);
            }
        }
        else if (!IsOutsideWater())
        {
            if (_isPlayerRoasting)
            {
                StopRoasting();
            }
            else if (_isPlayerSleeping)
            {
                StopSleeping();
            }
            _oneShotAudio.PlayOneShot(_waterExtinguishAudio);
            SetState(State.UNLIT);
        }
    }

    public void PackUp()
    {
        UpdateInsideShip(false);
        SetState(State.UNLIT, true);
        _lastOutsideWaterState = true;
        GetComponentInParent<PortableCampfireItem>().TogglePackUp(true);
    }

    public void UpdateProperties()
    {
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb && !rb.isKinematic)
        {
            _collider.convex = true;
        }
        else
        {
            _collider.convex = false;
        }
    }

    public void UpdateInsideShip(bool insideShip)
    {
        _insideShip = insideShip;
        if (!insideShip)
        {
            _reactorHeatMeter = 0f;
            _reactorHeatMeterLength = Random.Range(10f, 30f);
        }
    }

    public bool IsExtinguished()
    {
        return _extinguished;
    }

    public void SetExtinguished(bool value)
    {
        _extinguished = value;
    }

    public bool IsOutsideWater()
    {
        return !_fluidDetector.InFluidType(FluidVolume.Type.WATER);
    }

    public void UpdatePrompt()
    {
        _cancelPrompt.SetText(_extinguished ? _packUpText : _extinguishText);
    }

    public ScreenPrompt GetPrompt()
    {
        return _cancelPrompt;
    }

    public void SetPromptVisibility(bool value)
    {
        _cancelPrompt.SetVisibility(value);
    }

    private void OnShipSystemFailure()
    {
        if (_insideShip)
        {
            SetState(State.UNLIT);
        }
        _shipDestroyed = true;
    }

    private void OnEnable()
    {
        if (_fluidDetector.GetShape())
        {
            _fluidDetector.GetShape().SetActivation(true);
        }
        if (_fluidDetector.GetCollider())
        {
            _fluidDetector.GetCollider().enabled = true;
        }
        ShipEnhancements.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
        {
            bool outsideWater = IsOutsideWater();
            _lastOutsideWaterState = outsideWater;
            _interactVolume._screenPrompt.SetDisplayState(outsideWater ? ScreenPrompt.DisplayState.Normal : ScreenPrompt.DisplayState.GrayedOut);
        });
    }

    private void OnDisable()
    {
        if (_fluidDetector.GetShape())
        {
            _fluidDetector.GetShape().SetActivation(false);
        }
        if (_fluidDetector.GetCollider())
        {
            _fluidDetector.GetCollider().enabled = false;
        }
        EffectVolume[] volsToRemove = [.. _fluidDetector._activeVolumes];
        foreach (EffectVolume vol in volsToRemove)
        {
            vol._triggerVolume.RemoveObjectFromVolume(_fluidDetector.gameObject);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
