﻿using UnityEngine;

namespace ShipEnhancements;

public class TetherHookItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.TetherHookType;

    [SerializeField]
    private GameObject _connectionMesh;
    [SerializeField]
    private Transform _anchorPos;
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private AudioClip _attachTetherAudio;
    [SerializeField]
    private AudioClip _dropAudio;

    private Tether _tether;
    private Tether _activeTether;
    private FirstPersonManipulator _cameraManipulator;
    private ScreenPrompt _tetherPrompt;
    private bool _lastFocused = false;

    public override string GetDisplayName()
    {
        return "Tether Hook";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _tether = GetComponent<Tether>();
        _activeTether = _tether;
        _cameraManipulator = FindObjectOfType<FirstPersonManipulator>();
        _tetherPrompt = new ScreenPrompt(InputLibrary.interactSecondary, "Attach Tether", 0, ScreenPrompt.DisplayState.Normal, false);
        _connectionMesh.SetActive(false);

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        if (ShipEnhancements.InMultiplayer)
        {
            ShipEnhancements.QSBCompat.AddTetherHook(this);
        }
    }

    private void Update()
    {
        bool focused = _cameraManipulator.GetFocusedOWItem() == this;
        _tetherPrompt.SetVisibility(focused);
        if (_lastFocused != focused)
        {
            PatchClass.UpdateFocusedItems(focused);
            _lastFocused = focused;
        }
        if (focused && OWInput.IsNewlyPressed(InputLibrary.interactSecondary))
        {
            OnPressInteract();
        }
    }

    private void OnPressInteract()
    {
        // if untethered
        if (!_activeTether.IsTethered())
        {
            // if player is not tethered to anything
            if (!ShipEnhancements.Instance.playerTether)
            {
                _activeTether.CreateTether(SELocator.GetPlayerBody(), _anchorPos.localPosition, Vector3.zero);
                ShipEnhancements.Instance.playerTether = _activeTether;
                _connectionMesh.SetActive(true);
                _tetherPrompt.SetText("Detach Tether");
                PlayOneShotAudio(_attachTetherAudio, 0.6f);

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendAttachTether(id, this);
                    }
                }
            }
            // if player is tethered to a hook already
            else
            {
                _activeTether = ShipEnhancements.Instance.playerTether;
                _activeTether.TransferTether(GetComponentInParent<OWRigidbody>(), transform.parent.InverseTransformPoint(_anchorPos.position), this);
                ShipEnhancements.Instance.playerTether = null;
                PlayOneShotAudio(_attachTetherAudio, 0.6f);

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendTransferTether(id, this, _activeTether.GetHook());
                    }
                }
            }
        }
        // if tethered
        else
        {
            DisconnectTether();
        }
    }

    public void OnConnectTetherRemote(uint id)
    {
        var playerRemote = ShipEnhancements.QSBAPI.GetPlayerBody(id);
        _activeTether.CreateRemoteTether(playerRemote.transform, _anchorPos.localPosition, Vector3.zero);
        _connectionMesh.SetActive(true);
        _tetherPrompt.SetText("Detach Tether");
        PlayOneShotAudio(_attachTetherAudio, 0.6f);
    }

    public void DisconnectTether()
    {
        RunDisconnectTether();

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendDisconnectTether(id, this);
            }
        }
    }

    private void RunDisconnectTether()
    {
        _activeTether.DisconnectTether();
        _activeTether = _tether;
        _connectionMesh.SetActive(false);
        _tetherPrompt.SetText("Attach Tether");
        if (_activeTether == ShipEnhancements.Instance.playerTether)
        {
            ShipEnhancements.Instance.playerTether = null;
        }
    }

    public void DisconnectFromHook()
    {
        _activeTether = _tether;
        _connectionMesh.SetActive(false);
        _tetherPrompt.SetText("Attach Tether");
    }

    public void OnDisconnectTetherRemote()
    {
        RunDisconnectTether();
    }

    public void TransferToHook()
    {
        _connectionMesh.SetActive(true);
        _tetherPrompt.SetText("Detach Tether");
    }

    public void OnTransferRemote(Tether newTether)
    {
        _activeTether = newTether;
        _activeTether.TransferTether(GetComponentInParent<OWRigidbody>(), transform.parent.InverseTransformPoint(_anchorPos.position), this);
        PlayOneShotAudio(_attachTetherAudio, 0.6f);
    }

    public void SetTether(Tether newTether)
    {
        _activeTether = newTether;
    }

    public Tether GetTether()
    {
        return _tether;
    }

    public Tether GetActiveTether()
    {
        return _activeTether;
    }

    public Vector3 GetAttachPointOffset()
    {
        return _anchorPos.localPosition;
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        _tether.SetAttachedRigidbody(gameObject.GetAttachedOWRigidbody());
        Locator.GetPromptManager().AddScreenPrompt(_tetherPrompt, PromptPosition.Center, false);
        transform.localScale = Vector3.one;
        _audioSource.clip = _dropAudio;
        _audioSource.pitch = Random.Range(0.95f, 1.05f);
        _audioSource.Play();
    }

    public override void PickUpItem(Transform holdTranform)
    {
        if (_activeTether?.IsTethered() ?? false)
        {
            DisconnectTether();
        }
        base.PickUpItem(holdTranform);
        Locator.GetPromptManager().RemoveScreenPrompt(_tetherPrompt);
        transform.localScale = Vector3.one * 0.6f;
        transform.localPosition -= new Vector3(0f, 0.1f, 0f);
        _audioSource.Stop();
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        transform.localScale = Vector3.one * 0.7f;
    }

    public override void OnCompleteUnsocket()
    {
        base.OnCompleteUnsocket();
        transform.localScale = Vector3.one * 0.6f;
        transform.localPosition -= new Vector3(0f, 0.1f, 0f);
    }

    private void PlayOneShotAudio(AudioClip clip, float volume)
    {
        _audioSource.pitch = _audioSource.time > 0 ? _audioSource.pitch : Random.Range(0.95f, 1.05f);
        _audioSource.PlayOneShot(clip, volume);
    }

    private void OnShipSystemFailure()
    {
        if (!_activeTether.IsTethered()) return;

        ShipEnhancements.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
        {
            ShipEnhancements.WriteDebugMessage(gameObject.GetAttachedOWRigidbody().name);

            if (GetComponentInParent<ShipDetachableModule>()
                || GetComponentInParent<ShipDetachableLeg>())
            {
                _activeTether.UpdateTetherBody(gameObject.GetAttachedOWRigidbody(), _activeTether.GetHook() != this);
            }
        });
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);

        if (ShipEnhancements.InMultiplayer)
        {
            ShipEnhancements.QSBCompat.RemoveTetherHook(this);
        }
    }
}
