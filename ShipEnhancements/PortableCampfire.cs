using UnityEngine;

namespace ShipEnhancements;

public class PortableCampfire : Campfire
{
    [SerializeField]
    private MeshCollider _collider;

    [HideInInspector]
    public ScreenPrompt extinguishPrompt;
    [HideInInspector]
    public ScreenPrompt packUpPrompt;
    [HideInInspector]
    public bool extinguished = true;

    private bool _insideShip;
    private float _reactorHeatMeter;
    private float _reactorHeatMeterLength;
    private bool _shipDestroyed;

    public override void Awake()
    {
        base.Awake();

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        extinguishPrompt = new ScreenPrompt(InputLibrary.cancel, "Extinguish", 0, ScreenPrompt.DisplayState.Normal, false);
        packUpPrompt = new ScreenPrompt(InputLibrary.cancel, "Pack up", 0, ScreenPrompt.DisplayState.Normal, false);
        _reactorHeatMeter = 0f;
        _reactorHeatMeterLength = Random.Range(10f, 30f);
    }

    public void UpdateCampfire()
    {
        if (!_shipDestroyed && _insideShip && !extinguished)
        {
            ShipEnhancements.Instance.GetShipResources().DrainOxygen(10f * Time.deltaTime);
            _reactorHeatMeter += Time.deltaTime;
            if (_reactorHeatMeter >= _reactorHeatMeterLength)
            {
                _reactorHeatMeter = 0f;
                _reactorHeatMeterLength = Random.Range(10f, 30f);
                Locator.GetShipBody().GetComponentInChildren<ShipReactorComponent>().SetDamaged(true);
            }
        }
    }

    public void PackUp()
    {
        UpdateInsideShip(false);
        GetComponentInParent<PortableCampfireItem>().TogglePackUp(true);
    }

    public void UpdateProperties()
    {
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb && !rb.isKinematic)
        {
            _collider.convex = true;
        }
        else
        {
            _collider.convex = false;
        }
    }

    public void UpdateInsideShip(bool insideShip)
    {
        _insideShip = insideShip;
        if (!insideShip)
        {
            _reactorHeatMeter = 0f;
            _reactorHeatMeterLength = Random.Range(10f, 30f);
        }
    }

    private void OnShipSystemFailure()
    {
        SetState(State.UNLIT);
        _shipDestroyed = true;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
