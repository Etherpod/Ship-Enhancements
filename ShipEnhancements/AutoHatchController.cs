using UnityEngine;

namespace ShipEnhancements;

public class AutoHatchController : MonoBehaviour
{
    [SerializeField]
    InteractReceiver _interactReceiver;

    HatchController _hatchController;

    private void Start()
    {
        _hatchController = transform.parent.GetComponentInChildren<HatchController>();

        _interactReceiver.OnPressInteract += OnPressInteract;

        DisableInteraction();
    }

    private void OnPressInteract()
    {
        _interactReceiver.DisableInteraction();
        _hatchController.OpenHatch();
    }

    public void EnableInteraction()
    {
        _interactReceiver.EnableInteraction();
    }

    public void DisableInteraction()
    {
        _interactReceiver.DisableInteraction();
    }
}
