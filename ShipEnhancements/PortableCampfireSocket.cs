using UnityEngine;

namespace ShipEnhancements;

public class PortableCampfireSocket : SEItemSocket
{
    protected override ItemType GetAcceptableType()
    {
        return ShipEnhancements.Instance.PortableCampfireType;
    }

    public override void CreateItem()
    {
        base.CreateItem();

        FluidDetector detector = _socketedItem.GetComponentInChildren<FluidDetector>(true);
        if (detector != null)
        {
            detector.gameObject.SetActive(true);
        }

        string tempSetting = (string)ShipEnhancements.Settings.temperatureZonesAmount.GetProperty();
        if (tempSetting == "All" || tempSetting == "Hot")
        {
            Campfire campfire = _socketedItem.GetComponentInChildren<Campfire>();
            if (campfire != null)
            {
                GameObject campfireTempZone = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Campfire.prefab");
                Instantiate(campfireTempZone, campfire.transform.parent);
            }
        }
    }

    public override void CreateItemRemote(OWItem item, bool socketItem)
    {
        base.CreateItemRemote(item, socketItem);

        FluidDetector detector = item.GetComponentInChildren<FluidDetector>(true);
        if (detector != null)
        {
            detector.gameObject.SetActive(true);
        }

        string tempSetting = (string)ShipEnhancements.Settings.temperatureZonesAmount.GetProperty();
        if (tempSetting == "All" || tempSetting == "Hot")
        {
            Campfire campfire = item.GetComponentInChildren<Campfire>();
            if (campfire != null)
            {
                GameObject campfireTempZone = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/TemperatureZone_Campfire.prefab");
                Instantiate(campfireTempZone, campfire.transform.parent);
            }
        }
    }
}
