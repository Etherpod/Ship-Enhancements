using QSB.WorldSync;
using ShipEnhancements;

namespace ShipEnhancementsQSB;

public class QSBCockpitSwitch : WorldObject<CockpitSwitch>
{
    public override string ReturnLabel()
    {
        return $"{ToString()}" +
            $"\r\nState:{AttachedObject.IsOn()}";
    }

    public override void SendInitialState(uint to)
    {
        ShipEnhancements.ShipEnhancements.QSBCompat.SendSwitchState(to, AttachedObject, 
            AttachedObject.IsOn());
    }
}
