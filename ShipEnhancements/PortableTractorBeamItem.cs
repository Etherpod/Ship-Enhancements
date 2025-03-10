using UnityEngine;
using System.Collections;

namespace ShipEnhancements;

public class PortableTractorBeamItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.PortableTractorBeamType;

    [SerializeField] 
    private GameObject _tractorBeamObj;
    [SerializeField]
    private GameObject _displayedItemObj;
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private GameObject _beamVolumeParent;
    [SerializeField]
    private GameObject _regularBeamVolume;
    [SerializeField]
    private GameObject _turboBeamVolume;

    private EffectVolume[] _volumes;
    private ScreenPrompt _turboPrompt;
    private FirstPersonManipulator _cameraManipulator;
    private OWCamera _playerCam;
    private bool _socketed;
    private bool _lastFocused = false;
    private bool _turbo = false;

    private readonly string _enableTurboText = "Enable Turbo";
    private readonly string _disableTurboText = "Disable Turbo";

    public override string GetDisplayName()
    {
        return "Portable Tractor Beam";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _volumes = GetComponentsInChildren<EffectVolume>(true);
        _cameraManipulator = FindObjectOfType<FirstPersonManipulator>();
        _turboPrompt = new ScreenPrompt(InputLibrary.interactSecondary, _enableTurboText, 0, ScreenPrompt.DisplayState.Normal, false);
    }

    private void Start()
    {
        _playerCam = Locator.GetPlayerCamera();
        TogglePackUp(true);
        _socketed = true;
        _beamVolumeParent.SetActive(false);
        _turboBeamVolume.SetActive(false);
    }

    private void Update()
    {
        bool focused = _cameraManipulator.GetFocusedOWItem() == this;
        if (_lastFocused != focused)
        {
            PatchClass.UpdateFocusedItems(focused);
            _lastFocused = focused;
        }

        UpdatePromptVisibility();

        if (focused && OWInput.IsNewlyPressed(InputLibrary.interactSecondary))
        {
            ToggleTurbo(!_turbo);

            if (ShipEnhancements.InMultiplayer)
            {
                foreach (uint id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendTractorBeamTurbo(id, this, _turbo);
                }
            }
        }
    }

    private void UpdatePromptVisibility()
    {
        bool flag = _lastFocused && _playerCam.enabled && OWInput.IsInputMode(InputMode.Character | InputMode.ShipCockpit);
        if (flag != _turboPrompt.IsVisible())
        {
            _turboPrompt.SetVisibility(flag);
        }
    }

    public void ToggleTurbo(bool enable)
    {
        _turbo = enable;
        _regularBeamVolume.SetActive(!_turbo);
        _turboBeamVolume.SetActive(_turbo);
        _turboPrompt.SetText(_turbo ? _disableTurboText : _enableTurboText);
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        TogglePackUp(false);
        UpdateAttachedBody(parent.GetAttachedOWRigidbody());
        Locator.GetPromptManager().AddScreenPrompt(_turboPrompt, PromptPosition.Center, false);
        StartCoroutine(ActivationDelay());
        _audioSource.Stop();
        _audioSource.AssignAudioLibraryClip(AudioType.NomaiTractorBeamActivate);
        _audioSource.Play();
        _socketed = false;
        transform.localScale = Vector3.one;
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        StopAllCoroutines();
        Locator.GetPromptManager().RemoveScreenPrompt(_turboPrompt, PromptPosition.Center);
        _beamVolumeParent.SetActive(false);
        TogglePackUp(true);
        if (!_socketed)
        {
            _audioSource.Stop();
            _audioSource.AssignAudioLibraryClip(AudioType.NomaiTractorBeamDeactivate);
            _audioSource.Play();
        }
        _socketed = false;
        transform.localScale = Vector3.one * 0.5f;
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        _socketed = true;
        transform.localScale = Vector3.one * 0.5f;
    }

    private void TogglePackUp(bool packUp)
    {
        _tractorBeamObj.SetActive(!packUp);
        _displayedItemObj.SetActive(packUp);
    }

    private IEnumerator ActivationDelay()
    {
        yield return new WaitForSeconds(0.2f);
        _beamVolumeParent.SetActive(true);
    }

    private void UpdateAttachedBody(OWRigidbody body)
    {
        foreach (EffectVolume vol in _volumes)
        {
            vol.SetAttachedBody(body);
        }
    }
}
