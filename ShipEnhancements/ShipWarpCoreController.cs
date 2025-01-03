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
    private bool _warping = false;
    private bool _pressed = false;
    private float _buttonOffset = -0.121f;

    public override void Awake()
    {
        base.Awake();
        _shipBody = SELocator.GetShipBody();
        GlobalMessenger<OWRigidbody>.AddListener("ShipCockpitDetached", OnShipCockpitDetached);
    }

    private void Start()
    {
        _interactReceiver.ChangePrompt("Activate Return Warp");
        _warpEffect.transform.localPosition = _shipPivot.localPosition;
    }

    protected override void OnPressInteract()
    {
        _buttonTransform.localPosition = new Vector3(0, _buttonOffset, 0);
        _pressed = true;

        if (_receiver == null) return;

        if (PlayerState.IsInsideShip() || PlayerState.AtFlightConsole())
        {
            if (PlayerState.InBrambleDimension())
            {
                PlayerFogWarpDetector detector = Locator.GetPlayerDetector().GetComponent<PlayerFogWarpDetector>();
                FogWarpVolume[] volumes = detector._warpVolumes.ToArray();
                foreach (FogWarpVolume volume in volumes)
                {
                    detector.UntrackFogWarpVolume(volume);
                }
            }

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
        _warping = true;
    }

    protected override void OnReleaseInteract()
    {
        _buttonTransform.localPosition = Vector3.zero;
        _pressed = false;
        if (_warping)
        {
            _interactReceiver.DisableInteraction();
        }
        else
        {
            _interactReceiver.ResetInteraction();
        }
    }

    protected override void OnLoseFocus()
    {
        base.OnLoseFocus();
        if (_pressed)
        {
            OnReleaseInteract();
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
        _warping = false;
    }

    private void OnShipCockpitDetached(OWRigidbody body)
    {
        _shipBody = body;
        _warpEffect._warpedObjectGeometry = body.gameObject;
        _warpEffect.transform.localPosition = _cockpitPivot.localPosition;
        _receiver?.OnCockpitDetached(body);
    }

    public void SetReceiver(ShipWarpCoreReceiver receiver)
    {
        _receiver = receiver;
    }

    public void ActivateWarp()
    {
        OnPressInteract();
    }

    public bool IsWarping()
    {
        return _warping;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger<OWRigidbody>.RemoveListener("ShipCockpitDetached", OnShipCockpitDetached);
    }
}
