using UnityEngine;

namespace ShipEnhancements;

public class CockpitErnesto : MonoBehaviour
{
    [SerializeField]
    private InteractReceiver _conversationZone = null;

    private CharacterDialogueTree _dialogueTree;

    private void Awake()
    {
        _dialogueTree = _conversationZone.GetComponent<CharacterDialogueTree>();

        GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
    }

    private void Start()
    {
        if (ErnestoModListHandler.ActiveModList.Count > 0)
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_MULTIPLE_ERNESTOS", true);
        }
    }

    private void OnEnterFlightConsole(OWRigidbody body)
    {
        _conversationZone.DisableInteraction();
    }

    private void OnExitFlightConsole()
    {
        _conversationZone.EnableInteraction();
    }

    private void OnDestroy()
    {
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
    }
}
