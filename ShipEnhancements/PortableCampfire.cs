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
    [SerializeField]
    private AudioClip _packUpAudio;
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private GameObject _itemParent;

    private ScreenPrompt _cancelPrompt;
    private bool _extinguished = true;
    private string _packUpText = "Pack up";
    private string _extinguishText = "Extinguish";
    private bool _insideShip;
    private float _reactorHeatMeter;
    private float _reactorHeatMeterLength;
    private bool _shipDestroyed;
    private bool _lastOutsideWaterState = false;
    private ShipReactorComponent _reactor;
    private PortableCampfireItem _item;

    public override void Awake()
    {
        base.Awake();
        _item = _itemParent.GetComponent<PortableCampfireItem>();

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        if (true)
        {
            GlobalMessenger.AddListener("ShipHullDetached", OnShipHullDetached);
        }

        _cancelPrompt = new PriorityScreenPrompt(InputLibrary.cancel, "Pack up", 0, ScreenPrompt.DisplayState.Normal, false);
        _reactor = SELocator.GetShipDamageController()._shipReactorComponent;
        _reactorHeatMeterLength = Random.Range(10f, 30f);
        _reactorHeatMeter = _reactorHeatMeterLength;
    }

    public void UpdateCampfire()
    {
        if (!_shipDestroyed && _insideShip && !_extinguished)
        {
            float oxygenDrain = 10f * Time.deltaTime;
            SELocator.GetShipResources().DrainOxygen(oxygenDrain);

            if (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost())
            {
                if (_reactorHeatMeter > 0)
                {
                    _reactorHeatMeter -= Time.deltaTime;
                }
                else if (_reactor.GetComponentInParent<ShipBody>())
                {
                    _reactorHeatMeter = _reactorHeatMeterLength;
                    _reactorHeatMeterLength = Random.Range(10f, 30f);
                    if (!_reactor.isDamaged)
                    {
                        ErnestoDetectiveController.SetReactorCause("campfire");
                    }
                    _reactor.SetDamaged(true);

                    if (ShipEnhancements.InMultiplayer)
                    {
                        foreach (uint id in ShipEnhancements.PlayerIDs)
                        {
                            ShipEnhancements.QSBCompat.SendCampfireReactorDamaged(id, _item);
                        }
                    }

                    if (!SEAchievementTracker.FireHazard && ShipEnhancements.AchievementsAPI != null)
                    {
                        SEAchievementTracker.FireHazard = true;
                        ShipEnhancements.AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.FIRE_HAZARD");
                    }
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
            _audioSource.PlayOneShot(_waterExtinguishAudio);
            SetState(State.UNLIT);
        }
    }

    public void PackUp()
    {
        UpdateInsideShip(false);
        SetState(State.UNLIT, true);
        _lastOutsideWaterState = true;
        if (_isPlayerRoasting)
        {
            StopRoasting();
        }
        else if (_isPlayerSleeping)
        {
            StopSleeping();
        }
        _audioSource.PlayOneShot(_packUpAudio, 0.5f);
        _item.TogglePackUp(true);
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
        if (!insideShip && (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost()))
        {
            _reactorHeatMeterLength = Random.Range(10f, 30f);
            _reactorHeatMeter = _reactorHeatMeterLength;
        }
    }

    public void OnExtinguishInteract()
    {
        if (IsExtinguished())
        {
            Locator.GetPromptManager().RemoveScreenPrompt(GetPrompt(), PromptPosition.Center);
            PackUp();
        }
        else
        {
            SetState(State.UNLIT);
            _audioSource.PlayOneShot(_waterExtinguishAudio);
        }
    }

    public void OnRemoteReactorDamaged()
    {
        if (!SEAchievementTracker.FireHazard && ShipEnhancements.AchievementsAPI != null)
        {
            SEAchievementTracker.FireHazard = true;
            ShipEnhancements.AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.FIRE_HAZARD");
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
        if (_fluidDetector._activeVolumes == null) return true;
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

    public override bool CanSleepHereNow()
    {
        return base.CanSleepHereNow() && !PlayerState.OnQuantumMoon();
    }

    public OWItem GetItem()
    {
        return _item;
    }

    private void OnShipSystemFailure()
    {
        if (_insideShip)
        {
            SetState(State.UNLIT);
        }
        _shipDestroyed = true;
    }

    private void OnShipHullDetached()
    {
        if (_insideShip)
        {
            SetState(State.UNLIT);
        }
    }

    private void OnEnable()
    {
        if (_fluidDetector == null) return;

        if (_fluidDetector.GetShape())
        {
            _fluidDetector.GetShape().SetActivation(true);
        }
        if (_fluidDetector.GetCollider())
        {
            _fluidDetector.GetCollider().enabled = true;
        }

        if (_fluidDetector._activeVolumes != null)
        {
            ShipEnhancements.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                bool outsideWater = IsOutsideWater();
                _lastOutsideWaterState = outsideWater;
                _interactVolume._screenPrompt.SetDisplayState(outsideWater ? ScreenPrompt.DisplayState.Normal : ScreenPrompt.DisplayState.GrayedOut);
            });
        }
    }

    private void OnDisable()
    {
        if (_fluidDetector == null) return;

        if (_fluidDetector.GetShape())
        {
            _fluidDetector.GetShape().SetActivation(false);
        }
        if (_fluidDetector.GetCollider())
        {
            _fluidDetector.GetCollider().enabled = false;
        }

        if (_fluidDetector._activeVolumes != null)
        {
            EffectVolume[] volsToRemove = [.. _fluidDetector._activeVolumes];
            foreach (EffectVolume vol in volsToRemove)
            {
                vol._triggerVolume.RemoveObjectFromVolume(_fluidDetector.gameObject);
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        if (true)
        {
            GlobalMessenger.RemoveListener("ShipHullDetached", OnShipHullDetached);
        }
    }
}
