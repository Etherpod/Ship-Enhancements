using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;
using System.Collections.Generic;

namespace ShipEnhancements;

public class CockpitEffectController : MonoBehaviour
{
    [SerializeField]
    private Texture2D[] _rustTextures;
    [SerializeField]
    private Texture2D[] _dirtTextures;
    [SerializeField]
    private MeshRenderer _filthRenderer;
    [SerializeField]
    private MeshRenderer _iceRenderer;

    private Material _filthMat;
    private Material _iceMat;
    private int _rustTexIndex;
    private int _dirtTexIndex;
    private readonly int _rustTexPropID = Shader.PropertyToID("_RustTex");
    private readonly int _dirtTexPropID = Shader.PropertyToID("_DirtTex");
    private readonly int _rustCutoffPropID = Shader.PropertyToID("_RustCutoff");
    private readonly int _dirtCutoffPropID = Shader.PropertyToID("_DirtCutoff");
    private readonly int _iceCutoffPropID = Shader.PropertyToID("_FreezeFactor");
    private FluidDetector _cockpitDetector;
    private ShockLayerController _shockLayerController;
    private ReactorHeatController _reactorHeat;

    private float _rustProgression;
    private float _dirtBuildupProgression;
    private float _dirtBuildupTime;
    private bool _addDirtBuildup = false;
    private float _dirtClearTime = 3f;
    private bool _clearDirt = false;
    private float _iceBuildup = 0f;

    private readonly Dictionary<FluidVolume.Type, float> _fluidClearTimes = new()
    {
        { FluidVolume.Type.WATER, 5f },
        { FluidVolume.Type.GEYSER, 2f },
        { FluidVolume.Type.SAND, 12f },
        { FluidVolume.Type.CLOUD, 7f }
    };

    private void Awake()
    {
        _cockpitDetector = GetComponentInChildren<StaticFluidDetector>();
        _shockLayerController = SELocator.GetShipTransform().GetComponentInChildren<ShockLayerController>();
        _filthMat = _filthRenderer.sharedMaterial;
        _iceMat = _iceRenderer.sharedMaterial;

        _rustProgression = Mathf.Lerp(0.15f, 1f, (float)rustLevel.GetProperty());
        _dirtBuildupTime = (float)dirtAccumulationTime.GetProperty();

        if (_dirtBuildupTime != 0f)
        {
            _cockpitDetector.OnEnterFluidType += OnEnterFluidType;
            _cockpitDetector.OnExitFluidType += OnExitFluidType;
        }
    }

    private void Start()
    {
        _reactorHeat = SELocator.GetShipTransform().GetComponentInChildren<ReactorHeatController>();

        if ((float)rustLevel.GetProperty() > 0)
        {
            _filthMat.SetFloat(_rustCutoffPropID, _rustProgression);
            if (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost())
            {
                _rustTexIndex = Random.Range(0, _rustTextures.Length);
                _filthMat.SetTexture(_rustTexPropID, _rustTextures[_rustTexIndex]);
                _filthMat.SetTextureOffset(_rustTexPropID, new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)));
            }
        }
        else
        {
            _filthMat.SetFloat(_rustCutoffPropID, 0f);
        }

        _filthMat.SetFloat(_dirtCutoffPropID, 0f);

        if (_dirtBuildupTime != 0f && (float)maxDirtAccumulation.GetProperty() > 0f)
        {
            if (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost())
            {
                _dirtTexIndex = Random.Range(0, _dirtTextures.Length);
                _filthMat.SetTexture(_dirtTexPropID, _dirtTextures[_dirtTexIndex]);
                _filthMat.SetTextureOffset(_dirtTexPropID, new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)));
            }
        }
        else
        {
            _dirtBuildupTime = 0f;
            //enabled = false;
        }

        _iceMat.SetFloat(_iceCutoffPropID, 0f);
    }

    private void Update()
    {
        if (_dirtBuildupTime == 0f && !_reactorHeat)
        {
            enabled = false;
            return;
        }

        if (_dirtBuildupTime != 0f)
        {
            UpdateDirt();
        }
        if (_reactorHeat && SELocator.GetShipTemperatureDetector().GetTemperatureRatio() < 0f)
        {
            UpdateIce();
        }
    }

    private void UpdateIce()
    {
        float tempRatio = SELocator.GetShipTemperatureDetector().GetTemperatureRatio() * -1;
        tempRatio *= 1.2f;
        float ratio = _reactorHeat.GetHeatRatio() * 1.5f * tempRatio;
        _iceBuildup = Mathf.Lerp(_iceBuildup, ratio, Time.deltaTime / 60f);
        _iceMat.SetFloat(_iceCutoffPropID, _iceBuildup);
    }

    private void UpdateDirt()
    {
        float clearTime = _dirtClearTime;

        float spinSpeed = SELocator.GetShipBody().GetAngularVelocity().sqrMagnitude;
        float spinPercent = Mathf.InverseLerp(ShipEnhancements.Instance.levelOneSpinSpeed * ShipEnhancements.Instance.levelOneSpinSpeed,
            ShipEnhancements.Instance.levelTwoSpinSpeed * ShipEnhancements.Instance.levelTwoSpinSpeed, spinSpeed);

        float shockPercent = 0f;

        if (_shockLayerController.enabled && _shockLayerController._ruleset != null)
        {
            if (_shockLayerController._ruleset.GetShockLayerType() == ShockLayerRuleset.ShockType.Atmospheric)
            {
                Vector3 toCenter = _shockLayerController._ruleset.GetRadialCenter().position - _shockLayerController._owRigidbody.GetPosition();
                float centerDist = toCenter.magnitude;
                float radiusMultiplier = 1f - Mathf.InverseLerp(_shockLayerController._ruleset.GetInnerRadius(),
                    _shockLayerController._ruleset.GetOuterRadius(), centerDist);

                Vector3 relativeFluidVelocity = _shockLayerController._fluidDetector.GetRelativeFluidVelocity();
                float velocityMagnitude = relativeFluidVelocity.magnitude;
                float minSpeed = _shockLayerController._ruleset.GetMinShockSpeed();
                float maxSpeed = _shockLayerController._ruleset.GetMaxShockSpeed();
                float shockSpeedPercent = Mathf.InverseLerp(minSpeed + ((maxSpeed - minSpeed) / 4), maxSpeed, velocityMagnitude);
                shockSpeedPercent *= radiusMultiplier;

                shockPercent = shockSpeedPercent;
            }
        }
        
        if (_clearDirt || spinPercent > 0 || shockPercent > 0)
        {
            if (spinPercent > 0)
            {
                float spinClearTime = Mathf.Lerp(15f, 3f, spinPercent);
                if (_clearDirt)
                {
                    clearTime = Mathf.Min(spinClearTime, clearTime);
                }
                else
                {
                    clearTime = spinClearTime;
                }
            }
            if (shockPercent > 0)
            {
                float shockClearTime = Mathf.Lerp(20f, 1f, shockPercent);
                if (_clearDirt || spinPercent > 0)
                {
                    clearTime = Mathf.Min(shockClearTime, clearTime);
                }
                else
                {
                    clearTime = shockClearTime;
                }
            }

            _filthMat.SetFloat(_dirtCutoffPropID, _dirtBuildupProgression);
            _dirtBuildupProgression = Mathf.Clamp(_dirtBuildupProgression - Time.deltaTime / clearTime 
                * Mathf.Sign(_dirtBuildupTime), 0f, (float)maxDirtAccumulation.GetProperty());
        }
        else if (_addDirtBuildup)
        {
            _filthMat.SetFloat(_dirtCutoffPropID, _dirtBuildupProgression);
            _dirtBuildupProgression = Mathf.Clamp(_dirtBuildupProgression
                + Time.deltaTime * (float)maxDirtAccumulation.GetProperty() / _dirtBuildupTime,
                0f, (float)maxDirtAccumulation.GetProperty());
        }
    }

    private void OnEnterFluidType(FluidVolume.Type type)
    {
        if (type == FluidVolume.Type.AIR)
        {
            _addDirtBuildup = true;
        }
        else if (_fluidClearTimes.ContainsKey(type))
        {
            _dirtClearTime = _fluidClearTimes[type];
            _clearDirt = true;
        }
    }

    private void OnExitFluidType(FluidVolume.Type type)
    {
        if (type == FluidVolume.Type.AIR)
        {
            _addDirtBuildup = false;
        }
        else if (_fluidClearTimes.ContainsKey(type))
        {
            foreach (FluidVolume.Type volType in _fluidClearTimes.Keys)
            {
                if (volType != type && _cockpitDetector.InFluidType(volType))
                {
                    return;
                }
            }
            _clearDirt = false;
        }
    }

    public void BroadcastCockpitEffectState()
    {
        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendInitialRustState(id, _rustTexIndex, _filthMat.GetTextureOffset(_rustTexPropID),
                    _dirtTexIndex, _filthMat.GetTextureOffset(_dirtTexPropID), _dirtBuildupProgression);
            }
        }
    }

    public void SetInitialEffectState(int rustIndex, Vector2 rustOffset, int dirtIndex, Vector2 dirtOffset, float dirtProgression)
    {
        _filthMat.SetTexture(_rustTexPropID, _rustTextures[rustIndex]);
        _filthMat.SetTextureOffset(_rustTexPropID, rustOffset);

        _filthMat.SetTextureOffset(_dirtTexPropID, dirtOffset);
        _dirtBuildupProgression = dirtProgression;
        _filthMat.SetFloat(_dirtCutoffPropID, 1 - _dirtBuildupProgression);
    }

    public void BroadcastDirtState()
    {
        if (ShipEnhancements.InMultiplayer && ShipEnhancements.QSBAPI.GetIsHost())
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendDirtState(id, _dirtBuildupProgression);
            }
        }
    }

    public void UpdateDirtState(float progression)
    {
        _dirtBuildupProgression = progression;
        _filthMat.SetFloat(_dirtCutoffPropID, progression);
    }
    
    private void OnDestroy()
    {
        _cockpitDetector.OnEnterFluidType -= OnEnterFluidType;
        _cockpitDetector.OnExitFluidType -= OnExitFluidType;
    }
}
