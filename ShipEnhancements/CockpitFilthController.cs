using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;
using System.Collections.Generic;

namespace ShipEnhancements;

public class CockpitFilthController : MonoBehaviour
{
    [SerializeField]
    Texture2D[] _rustTextures;
    [SerializeField]
    MeshRenderer _rustRenderer;
    [SerializeField]
    MeshRenderer _dirtRenderer;
    [SerializeField]
    GameObject _rustParent;
    [SerializeField]
    GameObject _dirtParent;

    private Material _rustMat;
    private int _rustTexIndex;
    private float _rustProgression;
    private Material _dirtMat;
    private string _dirtLowerClipPropID = "_LowerClip";
    private string _dirtUpperClipPropID = "_UpperClip";
    private FluidDetector _cockpitDetector;
    private ShockLayerController _shockLayerController;

    private float _dirtBuildupProgression;
    private float _dirtBuildupTime;
    private bool _addDirtBuildup = false;
    private float _dirtClearTime = 3f;
    private bool _clearDirt = false;

    private readonly Dictionary<FluidVolume.Type, float> _fluidClearTimes = new()
    {
        { FluidVolume.Type.WATER, 5f },
        { FluidVolume.Type.GEYSER, 2f },
        { FluidVolume.Type.SAND, 20f },
        { FluidVolume.Type.CLOUD, 7f }
    };

    private void Awake()
    {
        _rustProgression = Mathf.Lerp(1f, 0.15f, (float)rustLevel.GetProperty());
        _dirtBuildupTime = (float)dirtAccumulationTime.GetProperty();
        _cockpitDetector = GetComponentInChildren<StaticFluidDetector>();
        _shockLayerController = SELocator.GetShipTransform().GetComponentInChildren<ShockLayerController>();

        if (_dirtBuildupTime > 0f)
        {
            _cockpitDetector.OnEnterFluidType += OnEnterFluidType;
            _cockpitDetector.OnExitFluidType += OnExitFluidType;
        }
    }

    private void Start()
    {
        if ((float)rustLevel.GetProperty() > 0)
        {
            _rustMat = _rustRenderer.sharedMaterial;
            _rustMat.SetFloat("_Cutoff", _rustProgression);
            if (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost())
            {
                _rustTexIndex = Random.Range(0, _rustTextures.Length);
                _rustMat.SetTexture("_MainTex", _rustTextures[_rustTexIndex]);
                _rustMat.SetTextureOffset("_MainTex", new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)));
            }
        }
        else
        {
            _rustParent.SetActive(false);
        }

        if (_dirtBuildupTime > 0f)
        {
            _dirtMat = _dirtRenderer.sharedMaterial;
            if (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost())
            {
                _dirtMat.SetTextureOffset("_MainTex", new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)));
                _dirtMat.SetFloat(_dirtLowerClipPropID, 1f);
                _dirtMat.SetFloat(_dirtUpperClipPropID, 1f);
            }
        }
        else
        {
            _dirtParent.SetActive(false);
            enabled = false;
        }
    }

    private void Update()
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

            if (_dirtMat.GetFloat(_dirtUpperClipPropID) < 1f)
            {
                _dirtMat.SetFloat(_dirtUpperClipPropID, Mathf.Lerp(1f, 0.5f, (_dirtBuildupProgression - 0.5f) * 2));
            }
            else if (_dirtMat.GetFloat(_dirtLowerClipPropID) < 1f)
            {
                _dirtMat.SetFloat(_dirtLowerClipPropID, Mathf.Lerp(1f, 0f, _dirtBuildupProgression * 2));
            }

            _dirtBuildupProgression = Mathf.Clamp01(_dirtBuildupProgression - Time.deltaTime / clearTime);
        }
        else if (_addDirtBuildup && _dirtBuildupProgression < (float)maxDirtAccumulation.GetProperty())
        {
            if (_dirtMat.GetFloat(_dirtLowerClipPropID) > 0f)
            {
                _dirtMat.SetFloat(_dirtLowerClipPropID, Mathf.Lerp(1f, 0f, _dirtBuildupProgression * 2));
            }
            else if (_dirtMat.GetFloat(_dirtUpperClipPropID) > 0f)
            {
                _dirtMat.SetFloat(_dirtUpperClipPropID, Mathf.Lerp(1f, 0.5f, (_dirtBuildupProgression - 0.5f) * 2));
            }

            _dirtBuildupProgression = Mathf.Clamp01(_dirtBuildupProgression + (Time.deltaTime * (float)maxDirtAccumulation.GetProperty()) / _dirtBuildupTime);
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

    public void BroadcastInitialRustState()
    {
        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendInitialRustState(id, _rustTexIndex, _rustMat.GetTextureOffset("_MainTex"));
            }
        }
    }

    public void SetInitialRustState(int textureIndex, Vector2 textureOffset)
    {
        //_rustMat = _rustRenderer.sharedMaterial;
        //_rustMat.SetFloat("_Cutoff", _rustProgression);
        _rustMat.SetTexture("_MainTex", _rustTextures[textureIndex]);
        _rustMat.SetTextureOffset("_MainTex", textureOffset);
    }

    public void BroadcastInitialDirtState()
    {
        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendInitialDirtState(id, _dirtMat.GetTextureOffset("_MainTex"), _dirtBuildupProgression);
            }
        }
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

    public void SetInitialDirtState(Vector2 textureOffset, float currentProgression)
    {
        _dirtMat = _dirtRenderer.sharedMaterial;
        _dirtMat.SetTextureOffset("_MainTex", textureOffset);
        _dirtBuildupProgression = currentProgression;
        _dirtMat.SetFloat(_dirtLowerClipPropID, Mathf.Lerp(1f, 0f, _dirtBuildupProgression * 2));
        _dirtMat.SetFloat(_dirtUpperClipPropID, Mathf.Lerp(1f, 0.5f, (_dirtBuildupProgression - 0.5f) * 2));
    }

    public void UpdateDirtState(float progression)
    {
        _dirtBuildupProgression = progression;
        _dirtMat.SetFloat(_dirtLowerClipPropID, Mathf.Lerp(1f, 0f, _dirtBuildupProgression * 2));
        _dirtMat.SetFloat(_dirtUpperClipPropID, Mathf.Lerp(1f, 0.5f, (_dirtBuildupProgression - 0.5f) * 2));
    }
    
    private void OnDestroy()
    {
        _cockpitDetector.OnEnterFluidType -= OnEnterFluidType;
        _cockpitDetector.OnExitFluidType -= OnExitFluidType;
    }
}
