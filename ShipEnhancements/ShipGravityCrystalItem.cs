using System;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipGravityCrystalItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.GravityCrystalType;

    [SerializeField]
    private DirectionalForceVolume _forceVolume;
    [SerializeField]
    private GameObject _meshParent;
    [SerializeField]
    private GameObject _brokenMesh;

    private ShipGravityComponent _gravityComponent;
    private bool _hasBeenSocketed = false;

    public override string GetDisplayName()
    {
        return "Gravity Crystal";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _gravityComponent = SELocator.GetShipTransform().GetComponentInChildren<ShipGravityComponent>();

        if (!(bool)disableGravityCrystal.GetProperty())
        {
            _gravityComponent.OnDamaged += OnGravityDamaged;
            _gravityComponent.OnRepaired += OnGravityRepaired;
        }
    }

    private void Start()
    {
        _forceVolume.SetVolumeActivation(false);
        _brokenMesh.SetActive((bool)disableGravityCrystal.GetProperty());
        _meshParent.SetActive(false);
    }
    
    private void OnGravityDamaged(ShipComponent component)
    {
        _brokenMesh.SetActive(true);
    }

    private void OnGravityRepaired(ShipComponent component)
    {
        _brokenMesh.SetActive(false);
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);

        transform.localScale = Vector3.one;

        if (!(bool)disableGravityCrystal.GetProperty())
        {
            _forceVolume.SetAttachedBody(parent.GetAttachedOWRigidbody());
            _forceVolume.SetVolumeActivation(true);
        }
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);

        transform.localPosition = new Vector3(0f, -0.3f, 0f);
        transform.localScale = Vector3.one * 0.5f;

        _meshParent.SetActive(true);
        _gravityComponent.OnComponentDamaged();

        if (!(bool)disableGravityCrystal.GetProperty())
        {
            _forceVolume.SetVolumeActivation(false);
        }
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);

        if (_hasBeenSocketed)
        {
            transform.localScale = Vector3.one;

            _meshParent.SetActive(false);
            if (!_gravityComponent.isDamaged)
            {
                _gravityComponent.OnComponentRepaired();
            }
        }
        else
        {
            _hasBeenSocketed = true;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!(bool)disableGravityCrystal.GetProperty())
        {
            _gravityComponent.OnDamaged -= OnGravityDamaged;
            _gravityComponent.OnRepaired -= OnGravityRepaired;
        }
    }
}
