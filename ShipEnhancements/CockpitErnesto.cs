using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements;

public class CockpitErnesto : MonoBehaviour
{
    [SerializeField]
    private InteractReceiver _conversationZone = null;
    [SerializeField]
    private Text _commentText = null;

    private CharacterDialogueTree _dialogueTree;
    private List<int> _questions = [];
    private List<int> _availableQuestions = [];
    private Coroutine _currentComment = null;

    private readonly float _commentLifetime = 10f;

    private List<string> _availableHeavyImpactComments = [];
    private string[] _heavyImpactComments =
    [
        "You'd better watch where you're flying that thing.",
        "Ouch. That's gonna be tough to repair.",
        "Have you tried not crashing? I heard it helps prevent damage.",
        "How did you not see that there? It's like you hit it on purpose.",
        "And you call yourself a pilot?",
        "I don't remember anyone saying \"Fly fast and hit everything big.\"",
        "I bet Slate is gonna be mad about that."
    ];

    private List<string> _availableShockComments = [];
    private string[] _shockComments =
    [
        "I don't think that's going to recharge the ship.",
        "I'd appreciate it if you didn't try electrocuting us.",
        "Great. How am I going to watch TV if the power is out?",
        "Ouch.",
        "You know I'm sitting on a piece of metal, right?",
        "You'd better go fix that before the ship blows up."
    ];

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
        _availableHeavyImpactComments.AddRange(_heavyImpactComments);
        _availableShockComments.AddRange(_shockComments);

        if (ErnestoModListHandler.ActiveModList.Count > 0)
        {
            DialogueConditionManager.SharedInstance.SetConditionState("SE_MULTIPLE_ERNESTOS", true);
        }

        _commentText.gameObject.SetActive(false);
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
        if (_currentComment != null)
        {
            StopCoroutine(_currentComment);
            _commentText.gameObject.SetActive(false);
            _currentComment = null;
        }

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

    public void MakeComment(string comment)
    {
        if (_currentComment == null)
        {
            _commentText.text = comment;
            StopAllCoroutines();
            _currentComment = StartCoroutine(ShowDialogueBox());
        }
    }

    private IEnumerator ShowDialogueBox()
    {
        _commentText.gameObject.SetActive(true);

        yield return new WaitForSeconds(_commentLifetime);

        _commentText.gameObject.SetActive(false);
        _currentComment = null;
    }

    public void OnHeavyImpact()
    {
        string comment = _availableHeavyImpactComments[Random.Range(0, _availableHeavyImpactComments.Count)];
        MakeComment(comment);
        _availableHeavyImpactComments.Remove(comment);

        if (_availableHeavyImpactComments.Count == 0)
        {
            _availableHeavyImpactComments.AddRange(_heavyImpactComments);
        }
    }

    public void OnElectricalShock()
    {
        string comment = _availableShockComments[Random.Range(0, _availableShockComments.Count)];
        MakeComment(comment);
        _availableShockComments.Remove(comment);

        if (_availableShockComments.Count == 0)
        {
            _availableShockComments.AddRange(_shockComments);
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
