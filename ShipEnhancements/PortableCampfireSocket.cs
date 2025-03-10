using UnityEngine;

namespace ShipEnhancements;

public class PortableCampfireSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.PortableCampfireType;
    }

    protected override void CreateItem()
    {
        base.CreateItem();

        FluidDetector detector = _socketedItem.GetComponentInChildren<FluidDetector>(true);
        if (detector != null)
        {
            detector.gameObject.SetActive(true);
        }

        if ((string)ShipEnhancements.Settings.temperatureZonesAmount.GetProperty() == "All")
        {
            Campfire campfire = _socketedItem.GetComponentInChildren<Campfire>();
            if (campfire != null)
            {
                GameObject campfireTempZone = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Campfire.prefab");
                Instantiate(campfireTempZone, campfire.transform.parent);
            }
        }
    }
}
