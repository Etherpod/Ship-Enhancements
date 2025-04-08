using QSB.WorldSync;
using ShipEnhancements;

namespace ShipEnhancementsQSB;

public class QSBCockpitButton : WorldObject<CockpitButton>
{
    public override string ReturnLabel()
    {
        return $"{ToString()}" +
            $"\r\nState:{AttachedObject.IsOn()}";
    }

    public override void SendInitialState(uint to)
    {
        if (AttachedObject is CockpitButtonSwitch)
        {
            CockpitButtonSwitch attached = AttachedObject as CockpitButtonSwitch;
            ShipEnhancements.ShipEnhancements.QSBCompat.SendButtonSwitchState(to, attached,
                attached.IsOn(), attached.IsActivated());
        }
        else
        {
            ShipEnhancements.ShipEnhancements.QSBCompat.SendButtonState(to, AttachedObject,
                AttachedObject.IsOn());
        }
    }
}
