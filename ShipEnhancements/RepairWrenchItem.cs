using UnityEngine;

namespace ShipEnhancements;

public class RepairWrenchItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.RepairWrenchType;

    [SerializeField]
    private OWRenderer _wrenchRenderer;

    private readonly int _batteryPropID = Shader.PropertyToID("_Battery");
    private float _batteryLevel;
    private bool _repairLimitEnabled = false;

    public override string GetDisplayName()
    {
        return "Repair Wrench";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
    }

    private void Start()
    {
        _batteryLevel = 1f;

        if (!ShipRepairLimitController.CanRepair())
        {
            UpdateBatteryLevel(0f);
        }
        else if (ShipRepairLimitController.GetRepairLimit() > 0)
        {
            foreach (ShipComponent component in SELocator.GetShipDamageController()._shipComponents)
            {
                component.OnRepaired += ctx => OnShipPartRepaired();
            }
            foreach (ShipHull hull in SELocator.GetShipDamageController()._shipHulls)
            {
                hull.OnRepaired += ctx => OnShipPartRepaired();
            }
            _repairLimitEnabled = true;
        }
    }

    public void UpdateBatteryLevel(float battery)
    {
        _batteryLevel = Mathf.Clamp01(battery);
        ShipEnhancements.WriteDebugMessage(_batteryLevel);
        _wrenchRenderer.SetMaterialProperty(_batteryPropID, _batteryLevel);
    }

    private void OnShipPartRepaired()
    {
        int repairLimit = ShipRepairLimitController.GetRepairLimit();
        float ratio = (repairLimit - ShipRepairLimitController.GetPatsRepaired()) / (float)repairLimit;
        UpdateBatteryLevel(ratio);
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        transform.localRotation = Quaternion.Euler(new Vector3(-70, 0, 0));
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (_repairLimitEnabled)
        {
            foreach (ShipComponent component in SELocator.GetShipDamageController()._shipComponents)
            {
                component.OnRepaired -= ctx => OnShipPartRepaired();
            }
            foreach (ShipHull hull in SELocator.GetShipDamageController()._shipHulls)
            {
                hull.OnRepaired -= ctx => OnShipPartRepaired();
            }
        }
    }
}
