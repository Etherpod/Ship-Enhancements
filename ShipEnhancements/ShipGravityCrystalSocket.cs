using System;
using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipGravityCrystalSocket : OWItemSocket
{
    private List<GameObject> _componentMeshes = [];
    private ShipGravityComponent _gravityComponent;
    private OWCollider _collider;
    private GameObject _socketedShadowCaster;
    private GameObject _removedShadowCaster;

    public override void Awake()
    {
        Reset();
        _sector = SELocator.GetShipSector();
        base.Awake();
        _acceptableType = ShipGravityCrystalItem.ItemType;
        _gravityComponent = SELocator.GetShipTransform().GetComponentInChildren<ShipGravityComponent>();
        _collider = gameObject.GetAddComponent<OWCollider>();
        _socketedShadowCaster = SELocator.GetShipTransform().Find("Module_Engine/Geo_Engine/ShadowCaster_Engine").gameObject;
        Mesh altShadowMesh = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/AltShadowCasters/ShadowCaster_Engine_NoGravCrystal.fbx").GetComponent<MeshFilter>().mesh;
        _removedShadowCaster = Instantiate(_socketedShadowCaster, _socketedShadowCaster.transform.parent);
        _removedShadowCaster.GetComponent<MeshFilter>().mesh = altShadowMesh;

        _gravityComponent.OnDamaged += OnGravityDamaged;
        _gravityComponent.OnRepaired += OnGravityRepaired;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
    }

    public override void Start()
    {
        base.Start();
    }

    public void AddComponentMeshes(GameObject[] meshes)
    {
        _componentMeshes.AddRange(meshes);
    }

    private void OnGravityDamaged(ShipComponent component)
    {
        if (ShipRepairLimitController.CanRepair() && !SELocator.GetShipDamageController().IsSystemFailed())
        {
            _collider.SetActivation(false);
        }
    }

    private void OnGravityRepaired(ShipComponent component)
    {
        _collider.SetActivation(true);
    }

    private void OnShipSystemFailure()
    {
        _collider.SetActivation(true);
    }

    public override bool PlaceIntoSocket(OWItem item)
    {
        /*if (item is ShipGravityCrystalItem && _socketedItem == null)
        {
            (item as ShipGravityCrystalItem).OnPlaceIntoShipSocket();
        }*/

        bool result = base.PlaceIntoSocket(item);
        if (result)
        {
            foreach (GameObject obj in _componentMeshes)
            {
                obj.SetActive(true);
            }
            _socketedShadowCaster.SetActive(true);
            //_removedShadowCaster.SetActive(false);
        }
        return result;
    }

    public override OWItem RemoveFromSocket()
    {
        OWItem result = base.RemoveFromSocket();
        if (result != null)
        {
            foreach (GameObject obj in _componentMeshes)
            {
                obj.SetActive(false);
            }
            _socketedShadowCaster.SetActive(false);
            _removedShadowCaster.SetActive(true);
        }
        return result;
    }

    private void OnDestroy()
    {
        _gravityComponent.OnDamaged -= OnGravityDamaged;
        _gravityComponent.OnRepaired -= OnGravityRepaired;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
