namespace ShipEnhancements;

public class PersistentInputButton : CockpitButtonSwitch
{
    private ShipPersistentInput _persistentInput;

    protected override void Start()
    {
        base.Start();

        _persistentInput = SELocator.GetShipBody().GetComponent<ShipPersistentInput>();
    }

    public override void OnChangeActiveEvent()
    {
        if (IsActivated())
        {
            _persistentInput.StartRecordingInput();
        }
        else if (IsOn())
        {
            _persistentInput.StopRecordingInput();
        }
    }

    public override void OnChangeStateEvent()
    {
        _persistentInput.SetInputEnabled(IsOn());
    }

    public override void SetPowered(bool powered)
    {
        base.SetPowered(powered);
        if (_electricalDisrupted) return;
        _persistentInput.SetInputEnabled(powered && IsOn());
    }
}
