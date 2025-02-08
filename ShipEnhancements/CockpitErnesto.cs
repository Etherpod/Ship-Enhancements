using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class CockpitErnesto : MonoBehaviour
{
    [SerializeField]
    private InteractReceiver _conversationZone = null;

    private CharacterDialogueTree _dialogueTree;
    private List<int> _questions = [];
    private List<int> _availableQuestions = [];

    private void Awake()
    {
        _dialogueTree = _conversationZone.GetComponent<CharacterDialogueTree>();

        _dialogueTree.OnStartConversation += OnStartConversation;
        GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);

        ResetAvailableQuestions();
    }

    private void Start()
    {
        if (ErnestoModListHandler.ActiveModList.Count > 0)
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_MULTIPLE_ERNESTOS", true);
        }
    }

    private void ResetAvailableQuestions()
    {
        _questions.Clear();
        _availableQuestions.Clear();

        for (int i = 1; i < 23; i++)
        {
            _questions.Add(i);
        }

        if (DialogueConditionManager.SharedInstance.GetConditionState("SE_ERNESTO_GESWALDO_PART_ONE"))
        {
            _questions.Add(101);
        }
        else
        {
            _questions.Add(100);
        }

        _availableQuestions.AddRange(_questions);
    }

    private void OnStartConversation()
    {
        foreach (int i in _questions)
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_ERNESTO_OPTION_" + i, false);
        }

        int num = _availableQuestions[Random.Range(0, _availableQuestions.Count)];
        DialogueConditionManager.SharedInstance.SetConditionState("SE_ERNESTO_OPTION_" + num, true);
        _availableQuestions.Remove(num);

        if (_availableQuestions.Count == 0)
        {
            ResetAvailableQuestions();
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
        _dialogueTree.OnStartConversation -= OnStartConversation;
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
    }
}
