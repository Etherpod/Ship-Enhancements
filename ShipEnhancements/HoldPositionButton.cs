namespace ShipEnhancements;

public class HoldPositionButton() : PidAutopilotButton(PidMode.HoldPosition)
{
    protected override bool CanActivate()
    {
        return base.CanActivate() && !SELocator.GetAutopilotPanelController().IsAutopilotActive();
    }
}