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
        "I bet Slate is gonna be mad about that.",
        "Hey, that was my favorite part!"
    ];

    private List<string> _availableShockComments = [];
    private string[] _shockComments =
    [
        "I don't think that's going to recharge the ship.",
        "I'd appreciate it if you didn't try electrocuting us.",
        "Great. How am I going to watch TV if the power is out?",
        "Ouch.",
        "You know I'm sitting on a piece of metal, right?",
        "That came as a shock. Ha!",
        "Good job. Now go fix the electricity so I can watch my TV."
    ];

    private List<string> _availableEjectComments = [];
    private string[] _ejectComments =
    [
        "What'd you do that for?",
        "Nice job, you just ruined a perfectly good cockpit.",
        "Good idea. You have a plan for what to do next, right?",
        "Maybe you can screw the cockpit back on.",
        "This wasn't exactly the space adventure I had in mind.",
        "Well, at least now we know it works.",
    ];

    private List<string> _availableReactorComments = [];
    private string[] _reactorComments =
    [
        "You might want to check on your reactor.",
        "Is it just me, or is it getting hot in here? It might just be me.",
        "Isn't there supposed to an alarm going off or something?",
        "Is your reactor supposed to be glowing red?"
    ];

    private void Awake()
    {
        _dialogueTree = _conversationZone.GetComponent<CharacterDialogueTree>();

        _dialogueTree.OnStartConversation += OnStartConversation;
        _dialogueTree.OnEndConversation += OnEndConversation;
        GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);

        ResetAvailableQuestions();
    }

    private void Start()
    {
        _availableHeavyImpactComments.AddRange(_heavyImpactComments);
        _availableShockComments.AddRange(_shockComments);
        _availableEjectComments.AddRange(_ejectComments);
        _availableReactorComments.AddRange(_reactorComments);

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

    private void OnEndConversation()
    {
        if (PlayerState.AtFlightConsole())
        {
            _conversationZone.DisableInteraction();
        }
    }

    public void MakeComment(string comment)
    {
        if (ShipEnhancements.InMultiplayer && !ShipEnhancements.QSBAPI.GetIsHost()) return;

        if (_currentComment == null)
        {
            _commentText.text = comment;
            StopAllCoroutines();
            _currentComment = StartCoroutine(ShowDialogueBox());

            if (ShipEnhancements.InMultiplayer)
            {
                foreach (uint id in ShipEnhancements.PlayerIDs)
                {
                    ShipEnhancements.QSBCompat.SendErnestoComment(id, comment);
                }
            }
        }
    }

    public void MakeCommentRemote(string comment)
    {
        _commentText.text = comment;
        StopAllCoroutines();
        _currentComment = StartCoroutine(ShowDialogueBox());
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

    public void OnCockpitDetached()
    {
        string comment = _availableEjectComments[Random.Range(0, _availableEjectComments.Count)];
        MakeComment(comment);
        _availableEjectComments.Remove(comment);

        if (_availableEjectComments.Count == 0)
        {
            _availableEjectComments.AddRange(_ejectComments);
        }
    }

    public void ReactorDamagedComment()
    {
        string comment = _availableReactorComments[Random.Range(0, _availableReactorComments.Count)];
        MakeComment(comment);
        _availableReactorComments.Remove(comment);

        if (_availableReactorComments.Count == 0)
        {
            _availableReactorComments.AddRange(_reactorComments);
        }
    }

    private void OnEnterFlightConsole(OWRigidbody body)
    {
        _conversationZone.DisableInteraction();

        if ((bool)ShipEnhancements.Settings.disableDamageIndicators.GetProperty() && Random.value < 0.25f)
        {
            ShipReactorComponent reactor = SELocator.GetShipTransform().GetComponentInChildren<ShipReactorComponent>();
            if (reactor != null && reactor.isDamaged)
            {
                ReactorDamagedComment();
            }
        }
    }

    private void OnExitFlightConsole()
    {
        _conversationZone.EnableInteraction();
    }

    private void OnDestroy()
    {
        _dialogueTree.OnStartConversation -= OnStartConversation;
        _dialogueTree.OnEndConversation -= OnEndConversation;
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
    }
}
