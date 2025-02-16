using UnityEngine;

namespace ShipEnhancements;

public class RepairWrenchItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.RepairWrenchType;

    [SerializeField]
    private OWRenderer _wrenchRenderer;

    private readonly int _batteryPropID = Shader.PropertyToID("_Battery");
    private float _batteryLevel;

    public override string GetDisplayName()
    {
        return "Repair Wrench";
    }

    private void Start()
    {
        _batteryLevel = 1f;
    }

    public void UpdateBatteryLevel(float battery)
    {
        _batteryLevel = Mathf.Clamp01(battery);
        _wrenchRenderer.SetMaterialProperty(_batteryPropID, _batteryLevel);
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        UpdateBatteryLevel(_batteryLevel - 0.1f);
    }
}
