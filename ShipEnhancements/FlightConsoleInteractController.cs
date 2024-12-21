using UnityEngine;

namespace ShipEnhancements;

public class FlightConsoleInteractController : MonoBehaviour
{
    private InteractZone _interactZone;
    private int _numFocused = 0;

    private void Awake()
    {
        _interactZone = GetComponent<InteractZone>();

        SELocator.SetFlightConsoleInteractController(this);
    }

    public void AddInteractible()
    {
        _numFocused++;
        UpdateZoneEnabled();
    }

    public void RemoveInteractible()
    {
        _numFocused--;
        UpdateZoneEnabled();
    }

    public void UpdateZoneEnabled()
    {
        ShipEnhancements.WriteDebugMessage(_numFocused);
        if (_numFocused > 0)
        {
            _interactZone.DisableInteraction();
        }
        else if (!PlayerState.AtFlightConsole())
        {
            if (!ShipEnhancements.InMultiplayer || !ShipEnhancements.QSBInteraction.FlightConsoleOccupied())
            {
                _interactZone.EnableInteraction();
            }
        }
    }
}
