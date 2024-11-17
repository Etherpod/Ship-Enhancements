using UnityEngine;

namespace ShipEnhancements;

public abstract class ProbePickupVolume : MonoBehaviour
{
    [SerializeField]
    protected InteractReceiver _interactReceiver;

    [HideInInspector]
    public static bool canRetrieveProbe = false;

    protected PlayerProbeLauncher _probeLauncher;
    protected SurveyorProbe _probe;

    protected virtual void Awake()
    {
        _probe = SELocator.GetProbe();
        _probeLauncher = FindObjectOfType<PlayerBody>().GetComponentInChildren<PlayerProbeLauncher>();

        _interactReceiver.OnPressInteract += OnPressInteract;
        _probe.OnLaunchProbe += OnLaunchProbe;
        _probe.OnRetrieveProbe += OnRetrieveProbe;
    }

    protected virtual void Start()
    {
        _interactReceiver.ChangePrompt("Pick up Scout");
    }

    protected virtual void OnPressInteract()
    {
        canRetrieveProbe = true;
        _probeLauncher.RetrieveProbe(true, false);
        _interactReceiver.DisableInteraction();
        canRetrieveProbe = false;
    }

    protected virtual void OnLaunchProbe() { }

    protected virtual void OnRetrieveProbe() { }

    protected virtual void OnDestroy()
    {
        _interactReceiver.OnPressInteract -= OnPressInteract;
        _probe.OnLaunchProbe -= OnLaunchProbe;
        _probe.OnRetrieveProbe -= OnRetrieveProbe;
    }
}
