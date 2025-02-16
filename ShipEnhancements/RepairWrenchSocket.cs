namespace ShipEnhancements;

public class RepairWrenchSocket : OWItemSocket
{
    public override void Awake()
    {
        Reset();
        _sector = SELocator.GetShipSector();
        base.Awake();
        _acceptableType = RepairWrenchItem.ItemType;

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        if ((bool)ShipEnhancements.Settings.preventSystemFailure.GetProperty())
        {
            GlobalMessenger.AddListener("ShipHullDetached", OnShipSystemFailure);
        }
    }

    private void OnShipSystemFailure()
    {
        _sector = null;
        _socketedItem?.SetSector(null);
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        if ((bool)ShipEnhancements.Settings.preventSystemFailure.GetProperty())
        {
            GlobalMessenger.RemoveListener("ShipHullDetached", OnShipSystemFailure);
        }
    }
}
