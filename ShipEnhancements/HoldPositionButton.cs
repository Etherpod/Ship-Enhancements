namespace ShipEnhancements;

public class HoldPositionButton() : PidAutopilotButton(PidMode.HoldPosition)
{
    protected override bool CanActivate()
    {
        return !SELocator.GetAutopilotPanelController().IsAutopilotActive();
    }
}