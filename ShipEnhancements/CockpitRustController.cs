using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class CockpitRustController : MonoBehaviour
{
    [SerializeField]
    Texture2D[] _rustTextures;
    [SerializeField]
    MeshRenderer _rustRenderer;
    [SerializeField]
    MeshRenderer _dirtRenderer;

    private Material _rustMat;
    private float _rustProgression;
    private Material _dirtMat;
    private string _dirtLowerClipPropID = "_LowerClip";
    private string _dirtUpperClipPropID = "_UpperClip";
    private ShipFluidDetector _shipDetector;

    private float _dirtBuildupProgression;
    private readonly float _dirtBuildupTime = 60f;
    private bool _addDirtBuildup = false;
    private readonly float _dirtClearTime = 3f;
    private bool _clearDirt = false;

    private void Awake()
    {
        _rustProgression = Mathf.Lerp(1f, 0.15f, (float)rustLevel.GetValue());
        _shipDetector = Locator.GetShipDetector().GetComponent<ShipFluidDetector>();

        _shipDetector.OnEnterFluidType += OnEnterFluidType;
        _shipDetector.OnExitFluidType += OnExitFluidType;
    }

    private void Start()
    {
        _rustMat = _rustRenderer.sharedMaterial;
        _rustMat.SetFloat("_Cutoff", _rustProgression);
        _rustMat.SetTexture("_MainTex", _rustTextures[Random.Range(0, _rustTextures.Length)]);
        _rustMat.SetTextureOffset("_MainTex", new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)));

        _dirtMat = _dirtRenderer.sharedMaterial;
        _dirtMat.SetTextureOffset("_MainTex", new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)));
        _dirtMat.SetFloat(_dirtLowerClipPropID, 1f);
        _dirtMat.SetFloat(_dirtUpperClipPropID, 1f);
    }

    private void Update()
    {
        if (_clearDirt)
        {
            if (_dirtMat.GetFloat(_dirtUpperClipPropID) < 1f)
            {
                _dirtMat.SetFloat(_dirtUpperClipPropID, Mathf.Lerp(1f, 0.5f, (_dirtBuildupProgression - 0.5f) * 2));
            }
            else if (_dirtMat.GetFloat(_dirtLowerClipPropID) < 1f)
            {
                _dirtMat.SetFloat(_dirtLowerClipPropID, Mathf.Lerp(1f, 0f, _dirtBuildupProgression * 2));
            }

            _dirtBuildupProgression = Mathf.Clamp01(_dirtBuildupProgression - Time.deltaTime / _dirtClearTime);
        }
        else if (_addDirtBuildup)
        {
            if (_dirtMat.GetFloat(_dirtLowerClipPropID) > 0f)
            {
                _dirtMat.SetFloat(_dirtLowerClipPropID, Mathf.Lerp(1f, 0f, _dirtBuildupProgression * 2));
            }
            else if (_dirtMat.GetFloat(_dirtUpperClipPropID) > 0f)
            {
                _dirtMat.SetFloat(_dirtUpperClipPropID, Mathf.Lerp(1f, 0.5f, (_dirtBuildupProgression - 0.5f) * 2));
            }

            _dirtBuildupProgression = Mathf.Clamp01(_dirtBuildupProgression + Time.deltaTime / _dirtBuildupTime);
        }
    }

    private void OnEnterFluidType(FluidVolume.Type type)
    {
        if (type == FluidVolume.Type.AIR)
        {
            _addDirtBuildup = true;
        }
        else if (type == FluidVolume.Type.WATER)
        {
            _clearDirt = true;
        }
    }

    private void OnExitFluidType(FluidVolume.Type type)
    {
        if (type == FluidVolume.Type.AIR)
        {
            _addDirtBuildup = false;
        }
        else if (type == FluidVolume.Type.WATER)
        {
            _clearDirt = false;
        }
    }
    
    private void OnDestroy()
    {
        _shipDetector.OnEnterFluidType -= OnEnterFluidType;
        _shipDetector.OnExitFluidType -= OnExitFluidType;
    }
}
