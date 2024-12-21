using UnityEngine;

namespace ShipEnhancements;

public class ShipWarpCoreController : CockpitInteractible
{
    [SerializeField]
    private Transform _buttonTransform;
    [SerializeField]
    private SingularityWarpEffect _warpEffect;
    [SerializeField]
    private Transform _shipPivot;
    [SerializeField]
    private Transform _cockpitPivot;

    private OWRigidbody _shipBody;
    private ShipWarpCoreReceiver _receiver;
    private bool _warpingWithPlayer = false;
    private readonly float _warpLength = 1f;

    public override void Awake()
    {
        base.Awake();
        _shipBody = SELocator.GetShipBody();
        GlobalMessenger<OWRigidbody>.AddListener("ShipCockpitDetached", OnShipCockpitDetached);
    }

    private void Start()
    {
        _interactReceiver.ChangePrompt("Activate Warp Core");
        _warpEffect.transform.localPosition = _shipPivot.localPosition;
    }

    private void Update()
    {
        if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary))
        {
            OnPressInteract();
        }
    }

    protected override void OnPressInteract()
    {
        _interactReceiver.DisableInteraction();
        if (PlayerState.IsInsideShip() || PlayerState.AtFlightConsole())
        {
            _warpingWithPlayer = true;
            _warpEffect.singularityController.OnCreation += WarpShip;
            _warpEffect.singularityController.Create();
            _receiver.PlayRecallEffect(_warpLength, _warpingWithPlayer);
        }
        else
        {
            _warpingWithPlayer = false;
            _warpEffect.OnWarpComplete += WarpShip;
            _warpEffect.WarpObjectOut(_warpLength);
        }
    }

    private void WarpShip()
    {
        if (_warpingWithPlayer)
        {
            _warpEffect.singularityController.OnCreation -= WarpShip;
            _warpEffect.singularityController.CollapseImmediate();
        }
        else
        {
            _warpEffect.OnWarpComplete -= WarpShip;
            _receiver.PlayRecallEffect(_warpLength, _warpingWithPlayer);
        }
        _receiver.WarpBodyToReceiver(_shipBody, _warpingWithPlayer);
        _interactReceiver.EnableInteraction();
    }

    private void OnShipCockpitDetached(OWRigidbody body)
    {
        _shipBody = body;
        _warpEffect._warpedObjectGeometry = body.gameObject;
        _warpEffect.transform.localPosition = _cockpitPivot.localPosition;
        _receiver.OnCockpitDetached(body);
    }

    public void SetReceiver(ShipWarpCoreReceiver receiver)
    {
        _receiver = receiver;
        ShipEnhancements.WriteDebugMessage(_receiver.transform.parent);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger<OWRigidbody>.RemoveListener("ShipCockpitDetached", OnShipCockpitDetached);
    }
}
