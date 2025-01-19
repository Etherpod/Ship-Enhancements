namespace ShipEnhancements;

public class ProbeLauncherComponent : ShipComponent
{
    private ShipProbeLauncherEffects _probeLauncherEffects;

    private void Start()
    {
        _componentName = ShipEnhancements.Instance.ProbeLauncherName;
    }

    public override void OnComponentDamaged()
    {
        _probeLauncherEffects?.SetDamaged(true);
    }

    public override void OnComponentRepaired()
    {
        _probeLauncherEffects?.SetDamaged(false);
    }

    public void SetProbeLauncherEffects(ShipProbeLauncherEffects volume)
    {
        _probeLauncherEffects = volume;
    }
}
