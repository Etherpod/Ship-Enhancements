using UnityEngine;

namespace ShipEnhancements;

public class CockpitCurtainController : MonoBehaviour
{
    [SerializeField]
    private GameObject _closedCurtainObj;
    [SerializeField]
    private GameObject _openCurtainObj;
    [SerializeField]
    private InteractReceiver _interactReceiver;

    private bool _open = true;

    private void Awake()
    {
        _interactReceiver.OnPressInteract += OnPressInteract;
    }

    private void Start()
    {
        UpdateCurtain();
    }

    private void OnPressInteract()
    {
        _open = !_open;
        UpdateCurtain();
    }

    private void UpdateCurtain()
    {
        _closedCurtainObj.SetActive(!_open);
        _openCurtainObj.SetActive(_open);
        _interactReceiver.ChangePrompt(_open ? "Close Curtain" : "Open Curtain");
    }
}
