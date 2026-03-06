using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MonoMod.Utils;
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
    private System.Random _random;
    private bool _bigHeadMode = false;
    private bool _showShipFailNextTime = false;
    private int _riddleConversationCountdown;
    private int _queryCount;
    private readonly int _maxQueries = 3;

    private int _questionCount;
    private readonly float _commentLifetime = 10f;

    private readonly List<string> _availableHeavyImpactComments = [];
    private readonly string[] _heavyImpactComments =
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

    private readonly List<string> _availableShockComments = [];
    private readonly string[] _shockComments =
    [
        "I don't think that's going to recharge the ship.",
        "I'd appreciate it if you didn't try electrocuting us.",
        "Great. How am I going to watch TV if the power is out?",
        "Ouch.",
        "You know I'm sitting on a piece of metal, right?",
        "That came as a shock. Ha!",
        "Good job. Now go fix the electricity so I can watch my TV."
    ];

    private readonly List<string> _availableEjectComments = [];
    private readonly string[] _ejectComments =
    [
        "What'd you do that for?",
        "Nice job, you just ruined a perfectly good cockpit.",
        "Good idea. You have a plan for what to do next, right?",
        "Maybe you can screw the cockpit back on.",
        "This wasn't exactly the space adventure I had in mind.",
        "Well, at least now we know it works.",
    ];

    private readonly List<string> _availableReactorComments = [];
    private readonly string[] _reactorComments =
    [
        "You might want to check on your reactor.",
        "Is it just me, or is it getting hot in here? It might just be me.",
        "Isn't there supposed to an alarm going off or something?",
        "Is your reactor supposed to be glowing red?"
    ];

    private readonly Dictionary<DeathType, List<string>> _availableDeathComments = [];

    private readonly Dictionary<DeathType, string[]> _deathComments = new()
    {
        {
            DeathType.Meditation,
            [
                "What? You're meditating already?",
                "Goodnight, sleep tight, don't let the anglerfish bite. Specifically me. I'm the anglerfish. I'm going to bite you.",
                "Wait, where are you going? You're just leaving me here? How am I going to survive on my own?",
                "Well, it's been nice knowing you for MINS minutes and SECS seconds."
            ]
        },
        {
            DeathType.Asphyxiation,
            [
                "You're suffocating here? You could have suffocated anywhere, and you wanted to suffocate here? Right next to me?",
                "Can you hurry it up? I'm trying to listen to a podcast right now. They're making some great points about the stock market situation in Dark Bramble and you're being very distracting."
            ]
        },
        {
            DeathType.Digestion,
            [
                "I've always wondered what my insides looked like."
            ]
        },
        {
            DeathType.Lava,
            [
                "Hey, isn't lava supposed to be deadly? Why are you touching it?"
            ]
        },
        {
            DeathType.TimeLoop,
            [
                "Hey, hatchling? I have something really important I need to tell you. Can you come here for a second?",
                "Wanna hear a fun fact? Just give me like 10 seconds to come up with one."
            ]
        },
        {
            DeathType.Impact,
            [
                "Uhh... are you okay?",
                "That killed you? Your bones must be incredibly fragile.",
                "Well, that's embarrasing."
            ]
        },
        {
            DeathType.Default,
            [
                "Wait! Don't die yet, I need to record this. It's gonna do numbers on Vine."
            ]
        },
        {
            DeathType.Crushed,
            [
                "What's that sound? Sounds like a beetle getting squished. A blue, four-eyed beetle.",
                "Are you being crushed by the weight of your responsibilities? Or was that for another time?"
            ]
        }
    };

    private void Awake()
    {
        _dialogueTree = _conversationZone.GetComponent<CharacterDialogueTree>();
        _random = new System.Random();

        _dialogueTree.OnStartConversation += OnStartConversation;
        _dialogueTree.OnEndConversation += OnEndConversation;
        GlobalMessenger<OWRigidbody>.AddListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.AddListener("ExitFlightConsole", OnExitFlightConsole);
        GlobalMessenger.AddListener("EnableBigHeadMode", OnEnableBigHeadMode);
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        GlobalMessenger<DeathType>.AddListener("PlayerDeath", OnPlayerDeath);

        // pull dialogue from github
        //var injection = (TextAsset)ShipEnhancements.LoadAsset("Assets/ShipEnhancements/TextAsset/TestErnestoQuestions.txt");
        var injection = ErnestoNetworkHandler.GetErnestoQuestions();
        var dialogue = _dialogueTree._xmlCharacterDialogueAsset;
        int index = dialogue.text.IndexOf("DIALOGUE_OPTION_PLACEHOLDER");
        if (index > 0)
        {
            var regex = new Regex(@"(<\/DialogueOption>)(?=[\s\S]*(DIALOGUE_BODY_SEPARATOR))");
            int matches = regex.Matches(injection.text).Count;
            ShipEnhancements.WriteDebugMessage("matches: " + matches);
            if (matches > 0)
            {
                _questionCount = matches;
                var splitIndex = injection.text.IndexOf("DIALOGUE_BODY_SEPARATOR");
                var injectOptions = injection.text.Substring(0, splitIndex);
                var injectBody = injection.text.Substring(splitIndex + 23);
                var newText = dialogue.text.Replace("DIALOGUE_OPTION_PLACEHOLDER", injectOptions);
                newText = newText.Replace("DIALOGUE_BODY_PLACEHOLDER", injectBody);
                _dialogueTree._xmlCharacterDialogueAsset = new TextAsset(newText);
            }
        }
        
        ResetAvailableQuestions();
    }

    private void Start()
    {
        _availableHeavyImpactComments.AddRange(_heavyImpactComments);
        _availableShockComments.AddRange(_shockComments);
        _availableEjectComments.AddRange(_ejectComments);
        _availableReactorComments.AddRange(_reactorComments);

        foreach (var (type, array) in _deathComments)
        {
            _availableDeathComments.Add(type, array.ToList());
        }

        /*if (ErnestoModListHandler.GetNumberErnestos() > 0)
        {
            SetConditionState("SE_MULTIPLE_ERNESTOS", true);
        }*/

        _commentText.gameObject.SetActive(false);
        _riddleConversationCountdown = _random.Next(5, 10);
    }

    private void ResetAvailableQuestions()
    {
        _questions.Clear();
        _availableQuestions.Clear();

        for (int i = 1; i <= _questionCount; i++)
        {
            _questions.Add(i);
        }

        /*if (GetConditionState("SE_ERNESTO_GESWALDO_PART_ONE"))
        {
            _questions.Add(101);
        }
        else
        {
            _questions.Add(100);
        }

        if ((bool)ShipEnhancements.Settings.addRadio.GetProperty())
        {
            _questions.Add(201);
        }
        if ((bool)ShipEnhancements.Settings.addShipClock.GetProperty())
        {
            _questions.Add(202);
        }
        if (PlayerData.GetPersistentCondition("SE_ERNESTO_TOLD_RIDDLE")
            && !PlayerData.GetPersistentCondition("SE_HAS_ERNESTONIAN_TRANSLATOR"))
        {
            _questions.Add(203);
        }*/
        
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

        if (_availableQuestions.Count > 0)
        {
            int num = _availableQuestions[_random.Next(0, _availableQuestions.Count)];
            SetConditionState("SE_ERNESTO_OPTION_" + num, true);
            _availableQuestions.Remove(num);
        }
        else if (!PlayerData.GetPersistentCondition("SE_ERNESTO_IS_AWARE") &&
            (!PlayerData.GetPersistentCondition("SE_ERNESTO_TOLD_RIDDLE") || 
                GetConditionState("SE_ERNESTO_REPEATED_RIDDLE")))
        {
            SetConditionState("SE_ERNESTO_FAILSAFE_QUESTION", true);
        }
        else
        {
            SetConditionState("SE_ERNESTO_FAILSAFE_QUESTION", false);
        }

        if (_showShipFailNextTime)
        {
            SetConditionState("SE_ERNESTO_SHIP_FAILURE", true);
            _showShipFailNextTime = false;
        }

        if (!GetConditionState("SE_ERNESTO_AWARE_NEXT_TIME")
            && !PlayerData.GetPersistentCondition("SE_ERNESTO_TOLD_RIDDLE"))
        {
            if (_riddleConversationCountdown > 0)
            {
                _riddleConversationCountdown--;
            }
            else
            {
                SetConditionState("SE_ERNESTO_RIDDLE", true);
            }
        }
    }

    private void OnEndConversation()
    {
        if (PlayerState.AtFlightConsole())
        {
            _conversationZone.DisableInteraction();
        }

        foreach (int i in _questions)
        {
            SetConditionState("SE_ERNESTO_OPTION_" + i, false);
        }

        if (_availableQuestions.Count == 0)
        {
            ResetAvailableQuestions();
        }

        if (GetConditionState("SE_ERNESTO_ASKED_QUESTION"))
        {
            _queryCount = Mathf.Min(_maxQueries, _queryCount + 1);
            SetConditionState("SE_ERNESTO_ASKED_QUESTION", false);
        }
        
        if (!GetConditionState("SE_ERNESTO_NO_QUESTIONS") && _queryCount >= _maxQueries)
        {
            SetConditionState("SE_ERNESTO_NO_QUESTIONS", true);
        }

        if (ErnestoNetworkHandler.GetNumberErnestos() > 0 && PlayerData.GetPersistentCondition("SE_KNOWS_ERNESTO") 
            && !PlayerData.GetPersistentCondition("SE_ERNESTO_IS_AWARE")
            && !GetConditionState("SE_ERNESTO_AWARE_NEXT_TIME"))
        {
            SetConditionState("SE_ERNESTO_AWARE_NEXT_TIME", true);
        }
        else if (GetConditionState("SE_ERNESTO_AWARE_NEXT_TIME"))
        {
            PlayerData.SetPersistentCondition("SE_ERNESTO_BECOME_AWARE", true);
            SetConditionState("SE_ERNESTO_AWARE_NEXT_TIME", false);
        }

        if (GetConditionState("SE_ERNESTO_GESWALDO_PART_TWO"))
        {
            SetConditionState("SE_ERNESTO_GESWALDO_PART_ONE", false);
            SetConditionState("SE_ERNESTO_GESWALDO_PART_TWO", false);
        }

        if (GetConditionState("SE_ERNESTO_EXPLODE_SHIP"))
        {
            SetConditionState("SE_ERNESTO_EXPLODE_SHIP", false);
            if (!SELocator.GetShipDamageController().IsSystemFailed())
            {
                if (GetConditionState("SE_ERNESTO_RIDDLE_EXPLOSION"))
                {
                    ErnestoDetectiveController.ItWasExplosion(fromRiddle: true);
                }
                else
                {
                    ErnestoDetectiveController.ItWasExplosion(fromErnesto: true);
                }
                
                SELocator.GetShipDamageController().Explode();
                SetConditionState("SE_ERNESTO_RIDDLE_EXPLOSION", false);
            }
            else
            {
                Locator.GetDeathManager().KillPlayer(DeathType.Lava);
            }
        }

        if (PlayerData.GetPersistentCondition("SE_ERNESTO_TOLD_RIDDLE"))
        {
            SetConditionState("SE_ERNESTO_RIDDLE", false);
        }

        if (PlayerData.GetPersistentCondition("SE_FOUND_ERNESTONIAN_TEXT")
            && PlayerData.GetPersistentCondition("SE_HAS_ERNESTONIAN_TRANSLATOR"))
        {
            PlayerData.SetPersistentCondition("SE_FOUND_ERNESTONIAN_TEXT", false);
        }

        SetConditionState("SE_ERNESTO_SHIP_FAILURE", false);
        SetConditionState("SE_ASKED_ERNESTONIAN", false);
        SetConditionState("SE_ASKED_POEM", false);
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
        string comment = _availableHeavyImpactComments[_random.Next(0, _availableHeavyImpactComments.Count)];
        MakeComment(comment);
        _availableHeavyImpactComments.Remove(comment);

        if (_availableHeavyImpactComments.Count == 0)
        {
            _availableHeavyImpactComments.AddRange(_heavyImpactComments);
        }
    }

    public void OnElectricalShock()
    {
        string comment = _availableShockComments[_random.Next(0, _availableShockComments.Count)];
        MakeComment(comment);
        _availableShockComments.Remove(comment);

        if (_availableShockComments.Count == 0)
        {
            _availableShockComments.AddRange(_shockComments);
        }
    }

    public void OnCockpitDetached()
    {
        string comment = _availableEjectComments[_random.Next(0, _availableEjectComments.Count)];
        MakeComment(comment);
        _availableEjectComments.Remove(comment);

        if (_availableEjectComments.Count == 0)
        {
            _availableEjectComments.AddRange(_ejectComments);
        }
    }

    public void ReactorDamagedComment()
    {
        string comment = _availableReactorComments[_random.Next(0, _availableReactorComments.Count)];
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
            ShipReactorComponent reactor = SELocator.GetShipDamageController()._shipReactorComponent;
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

    private void OnEnableBigHeadMode()
    {
        if (!_bigHeadMode)
        {
            _bigHeadMode = true;
            transform.Find("Beast_Anglerfish").transform.localScale *= 2f;
            transform.position += new Vector3(0f, 0.11f, 0f);
            SetConditionState("SE_ERNESTO_BIG_HEAD", true);
        }
    }

    private void OnShipSystemFailure()
    {
        _showShipFailNextTime = true;
    }

    private void OnPlayerDeath(DeathType deathType)
    {
        if (!_deathComments.ContainsKey(deathType)) return;
        
        var distSqr = (SELocator.GetPlayerBody().transform.position - 
            transform.position).sqrMagnitude;
        if (distSqr < 5f * 5f)
        {
            var comments = _availableDeathComments[deathType];
            if (comments.Count > 0)
            {
                var com = comments[_random.Next(0, comments.Count)];
                
                if (deathType == DeathType.Meditation)
                {
                    if (com.Contains("MINS"))
                    {
                        var mins = (int)(Time.timeSinceLevelLoad / 60f);
                        if (mins == 0)
                        {
                            com = com.Replace("MINS minutes and ", "");
                        }
                        else
                        {
                            com = com.Replace("MINS", mins.ToString());
                        }
                    }

                    if (com.Contains("SECS"))
                    {
                        var secs = (int)(Time.timeSinceLevelLoad % 60f);
                        if (secs == 0)
                        {
                            com = "Well, it's been nice knowing you for... zero seconds? That doesn't sound right.";
                        }
                        else
                        {
                            com = com.Replace("SECS", secs.ToString());
                        }
                    }
                }
                
                MakeComment(com);
                _availableDeathComments[deathType].Remove(com);

                if (_availableDeathComments[deathType].Count == 0)
                {
                    _availableDeathComments[deathType].AddRange(_deathComments[deathType]);
                }
            }
        }
    }

    private void OnDestroy()
    {
        _dialogueTree.OnStartConversation -= OnStartConversation;
        _dialogueTree.OnEndConversation -= OnEndConversation;
        GlobalMessenger<OWRigidbody>.RemoveListener("EnterFlightConsole", OnEnterFlightConsole);
        GlobalMessenger.RemoveListener("ExitFlightConsole", OnExitFlightConsole);
        GlobalMessenger.RemoveListener("EnableBigHeadMode", OnEnableBigHeadMode);
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        GlobalMessenger<DeathType>.RemoveListener("PlayerDeath", OnPlayerDeath);
    }

    private bool GetConditionState(string id)
    {
        return DialogueConditionManager.SharedInstance.GetConditionState(id);
    }

    private void SetConditionState(string id, bool value)
    {
        DialogueConditionManager.SharedInstance.SetConditionState(id, value);
    }
}
