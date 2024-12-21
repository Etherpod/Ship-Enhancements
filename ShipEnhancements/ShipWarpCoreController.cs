using System;
using UnityEngine;

namespace ShipEnhancements;

public class ShipWarpCoreController : MonoBehaviour
{
    [SerializeField]
    private Transform _buttonTransform;
    [SerializeField]
    private InteractReceiver _interactReceiver;
    [SerializeField]
    private SingularityWarpEffect _warpEffect;

    private OWRigidbody _shipBody;
    private ShipWarpCoreReceiver _receiver;
    private int _framesToReposition;
    private bool _focused = false;

    private void Awake()
    {
        _shipBody = SELocator.GetShipBody();
    }

    private void Start()
    {
        _interactReceiver.ChangePrompt("Activate Warp Core");
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;
    }

    private void OnPressInteract()
    {
        _interactReceiver.DisableInteraction();
        _warpEffect.singularityController.OnCreation += WarpShip;
        _warpEffect.singularityController.Create();
    }

    private void OnGainFocus()
    {
        if (!_focused)
        {
            _focused = true;
            SELocator.GetFlightConsoleInteractController().AddInteractible();
        }
    }

    private void OnLoseFocus()
    {
        if (_focused)
        {
            _focused = false;
            SELocator.GetFlightConsoleInteractController().RemoveInteractible();
        }
    }

    private void WarpShip()
    {
        _warpEffect.singularityController.OnCreation -= WarpShip;
        _warpEffect.singularityController.CollapseImmediate();
        _shipBody.WarpToPositionRotation(_receiver.GetWarpPosition().position, _receiver.GetWarpPosition().rotation);
        OWRigidbody newBody = _receiver.GetAttachedOWRigidbody();
        if (newBody != null)
        {
            _shipBody.SetVelocity(newBody.GetPointVelocity(_receiver.GetWarpPosition().position));
            _shipBody.SetAngularVelocity(newBody.GetAngularVelocity());
        }
        _interactReceiver.EnableInteraction();
    }

    public void SetReceiver(ShipWarpCoreReceiver receiver)
    {
        _receiver = receiver;
        ShipEnhancements.WriteDebugMessage(_receiver.transform.parent);
    }

    private void OnDestroy()
    {
        _interactReceiver.OnPressInteract -= OnPressInteract;
        _interactReceiver.OnGainFocus -= OnGainFocus;
        _interactReceiver.OnLoseFocus -= OnLoseFocus;
    }
}
