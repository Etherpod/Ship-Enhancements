using System;
using UnityEngine;

namespace ShipEnhancements;

public class ShipWarpCoreController : CockpitInteractible
{
    [SerializeField]
    private Transform _buttonTransform;
    [SerializeField]
    private SingularityWarpEffect _warpEffect;

    private OWRigidbody _shipBody;
    private ShipWarpCoreReceiver _receiver;

    public override void Awake()
    {
        base.Awake();
        _shipBody = SELocator.GetShipBody();
    }

    private void Start()
    {
        _interactReceiver.ChangePrompt("Activate Warp Core");
    }

    protected override void OnPressInteract()
    {
        _interactReceiver.DisableInteraction();
        _warpEffect.singularityController.OnCreation += WarpShip;
        _warpEffect.singularityController.Create();
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
}
