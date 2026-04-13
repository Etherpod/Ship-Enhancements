using UnityEngine;

namespace ShipEnhancements;

public class SignalscopeComponent : ShipComponent
{
    private Signalscope _signalscope;
    private SignalscopeAudioController _audioController;
    private Transform _dishTransform;
    private float _dishT;
    private Vector3 _startRotation;
    private AudioClip _initialStatic;
    private AudioClip _brokenStatic;

    private void Start()
    {
        _signalscope = Locator.GetPlayerCamera().GetComponentInChildren<Signalscope>();
        _audioController = _signalscope._signalscopeAudio;
        _dishTransform = SELocator.GetShipTransform().Find("Module_Cockpit/Geo_Cockpit/Cockpit_Tech/Cockpit_Tech_Exterior/SignalDishPivot");
        _componentName = ShipEnhancements.Instance.SignalscopeName;
        _initialStatic = _audioController._staticSource.clip;
        _brokenStatic = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/signalscope_brokenstatic.ogg");
        
        GlobalMessenger<Signalscope>.AddListener("EquipSignalscope", OnEquipSignalscope);
        GlobalMessenger.AddListener("UnequipSignalscope", OnUnequipSignalscope);
        enabled = false;
    }

    private void FixedUpdate()
    {
        if (_dishT < 1f)
        {
            _dishT = Mathf.Clamp01(_dishT + Time.deltaTime / 0.8f);
            _dishTransform.localEulerAngles = new Vector3(Mathf.SmoothStep(_startRotation.x, 0f, _dishT), 0f, 0f);
        }
        else
        {
            enabled = false;
        }
    }
    
    public void OnEquipSignalscope(Signalscope scope)
    {
        if (PlayerState.AtFlightConsole() && isDamaged)
        {
            _audioController._staticSource.Stop();
            _audioController._staticSource._audioLibraryClip = AudioType.None;
            _audioController._staticSource.clip = _brokenStatic;
            _audioController.PlaySignalscopeStatic();
        }
    }

    public void OnUnequipSignalscope()
    {
        if (PlayerState.AtFlightConsole() && isDamaged)
        {
            _audioController._staticSource.Stop();
            _audioController._staticSource.AssignAudioLibraryClip(AudioType.ToolScopeStatic);
            _audioController.StopSignalscopeStatic();
        }
    }

    public override void OnComponentDamaged()
    {
        enabled = false;
        _startRotation = _dishTransform.localEulerAngles;
        _dishT = 0f;

        _audioController._staticSource.Stop();
        _audioController._staticSource._audioLibraryClip = AudioType.None;
        _audioController._staticSource.clip = _brokenStatic;
        if (_signalscope.IsEquipped())
        {
            _audioController._staticSource.Play();
        }
    }

    public override void OnComponentRepaired()
    {
        if (_startRotation.x > 0f)
        {
            enabled = true;
        }
        
        _audioController._staticSource.Stop();
        _audioController._staticSource.AssignAudioLibraryClip(AudioType.ToolScopeStatic);
        if (_signalscope.IsEquipped())
        {
            _audioController._staticSource.Play();
        }
    }

    private void OnDestroy()
    {
        GlobalMessenger<Signalscope>.RemoveListener("EquipSignalscope", OnEquipSignalscope);
        GlobalMessenger.RemoveListener("UnequipSignalscope", OnUnequipSignalscope);
    }
}
