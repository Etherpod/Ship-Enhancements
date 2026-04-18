using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements;

public class ShipRemoteControl : MonoBehaviour
{
    [SerializeField]
    private SignalscopeCommandList _canvasList;
    [SerializeField]
    private Text _groupHeader;
    [SerializeField]
    private CanvasGroupAnimator _animator;
    [SerializeField]
    private GameObject _shipViewerParent;
    [SerializeField]
    private Image _viewerImage;
    
    private AudioSignal _shipAudioSignal;
    private ShipCommand _currentCommand;
    private ShipCommand.CommandGroup _groupMask;
    private ScreenPrompt _interactPrompt;
    private ScreenPrompt _cycleCommandPrompt;
    private ScreenPrompt _cycleGroupPrompt;
    private ScreenPrompt _changeCameraPrompt;

    private List<ShipViewerCamera> _shipCameras = [];
    private int _shipCameraIndex;
    private RenderTexture _shipViewerTexture;
    
    private InputMode _lastInputMode;
    private bool _scopeEquipped;
    private bool _visible;
    private bool _controllingShip;

    private List<ShipCommand> _commands = 
    [
        new ShipCommand_Explode(),
        new ShipCommand_EngineSwitch(),
        new ShipCommand_ReturnWarp(),
        new ShipCommand_HonkHorn(),
        new ShipCommand_CockpitEject(),
        new ShipCommand_EngineEject(),
        new ShipCommand_SuppliesEject(),
        new ShipCommand_LandingGearEject(),
        new ShipCommand_Autopilot(),
        new ShipCommand_OrbitAutopilot(),
        new ShipCommand_MatchVelocity(),
        new ShipCommand_HoldPosition(),
        new ShipCommand_RemoveTarget(),
        new ShipCommand_TargetPlayerPlanet(),
        new ShipCommand_TargetCurrentLockOn(),
        new ShipCommand_TargetPlayer(),
        new ShipCommand_TargetProbe(),
        new ShipCommand_CallErnesto(),
        new ShipCommand_ViewShip()
    ];

    private Dictionary<ShipCommand.CommandGroup, string> _groupToDisplayName = new()
    {
        { ShipCommand.CommandGroup.Components, "Component Commands" },
        { ShipCommand.CommandGroup.Modules, "Module Commands" },
        { ShipCommand.CommandGroup.Autopilot, "Autopilot Commands" },
        { ShipCommand.CommandGroup.LockOn, "Targeting Commands" },
        { ShipCommand.CommandGroup.Misc, "Other Commands" },
    };

    private Dictionary<ShipCommand.CommandGroup, List<ShipCommand>> _commandsByGroup = [];

    private void Awake()
    {
        foreach (var cmd in _commands)
        {
            if (!_commandsByGroup.ContainsKey(cmd.GetCommandGroup()))
            {
                _commandsByGroup.Add(cmd.GetCommandGroup(), [cmd]);
            }
            else
            {
                _commandsByGroup[cmd.GetCommandGroup()].Add(cmd);
            }
        }
        
        _groupMask = ShipCommand.CommandGroup.Components;
        _currentCommand = _commandsByGroup[_groupMask][0];
        _interactPrompt = new ScreenPrompt(InputLibrary.interact, "Tune In");
        _cycleCommandPrompt = new PriorityScreenPrompt(InputLibrary.up, InputLibrary.down, 
            "Cycle Command", ScreenPrompt.MultiCommandType.POS_NEG);
        _cycleGroupPrompt = new PriorityScreenPrompt(InputLibrary.left, InputLibrary.right, 
            "Cycle Group", ScreenPrompt.MultiCommandType.POS_NEG);
        _changeCameraPrompt = new PriorityScreenPrompt(InputLibrary.left, InputLibrary.right, 
            "Change Camera", ScreenPrompt.MultiCommandType.POS_NEG);

        // Locator should be ready by the time this obj gets created
        var canvas = GetComponent<Canvas>();
        canvas.worldCamera = Locator.GetPlayerCamera().mainCamera;
        canvas.planeDistance = 9f;
        AddMaterials();

        var launcherParent = SELocator.GetShipTransform().Find("Module_Cockpit/Systems_Cockpit/ProbeLauncher");
        var launcherCam = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ShipViewer_LauncherCamera.prefab");
        _shipCameras.Add(ShipEnhancements.CreateObject(launcherCam, launcherParent).GetComponent<ShipViewerCamera>());
        var landingParent = SELocator.GetShipTransform().Find("Module_Cockpit/Systems_Cockpit");
        var landingCam = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ShipViewer_LandingCamera.prefab");
        _shipCameras.Add(ShipEnhancements.CreateObject(landingCam, landingParent).GetComponent<ShipViewerCamera>());

        _shipViewerTexture = new RenderTexture(512, 512, 16);
        _shipViewerTexture.name = "ShipViewerTexture";
        _shipViewerTexture.hideFlags = HideFlags.HideAndDontSave;
        _shipViewerTexture.Create();
        
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
    }

    private void AddMaterials()
    {
        var refMat = GameObject.Find("PlayerHUD/HelmetOffUI/SignalscopeCanvas/SigScopeDisplay/FrequencyLabel")
            .GetComponent<Text>().material;
        
        foreach (var image in GetComponentsInChildren<Image>(true))
        {
            image.material = new Material(refMat);
        }
        foreach (var text in GetComponentsInChildren<Text>(true))
        {
            text.material = new Material(refMat);
        }
    }

    public void AssignErnestoCallDialogue(CharacterDialogueTree dialogueTree)
    {
        if (dialogueTree == null) return;

        foreach (var cmd in _commands)
        {
            if (cmd is ShipCommand_CallErnesto ernestoCmd)
            {
                ernestoCmd.AssignDialogue(dialogueTree);
                return;
            }
        }
    }

    private void Start()
    {
        foreach (AudioSignal signal in Locator.GetAudioSignals())
        {
            if (signal.GetName() == ShipEnhancements.Instance.ShipSignalName)
            {
                _shipAudioSignal = signal;
            }
        }
        
        _animator.SetImmediate(0f, new Vector3(1f, 0f, 1f));
    }

    private void Update()
    {
        if (Locator.GetToolModeSwapper().GetToolMode() == ToolMode.SignalScope
            && OWInput.IsInputMode(InputMode.Character | InputMode.SatelliteCam))
        {
            if (!_scopeEquipped)
            {
                Locator.GetPromptManager().AddScreenPrompt(_interactPrompt, PromptPosition.Center);
                Locator.GetPromptManager().AddScreenPrompt(_cycleCommandPrompt, PromptPosition.UpperRight);
                Locator.GetPromptManager().AddScreenPrompt(_cycleGroupPrompt, PromptPosition.UpperRight);
                Locator.GetPromptManager().AddScreenPrompt(_changeCameraPrompt, PromptPosition.UpperRight);
                _scopeEquipped = true;
            }

            _interactPrompt.SetVisibility(false);
            _cycleCommandPrompt.SetVisibility(false);
            _cycleGroupPrompt.SetVisibility(false);
            _changeCameraPrompt.SetVisibility(false);

            if (!_visible && _shipAudioSignal.GetSignalStrength() == 1f)
            {
                if (OWInput.IsNewlyPressed(InputLibrary.interact))
                {
                    SetVisible(true);
                    return;
                }

                _interactPrompt.SetVisibility(true);
            }
            
            if (_visible)
            {
                if (_shipAudioSignal.GetSignalStrength() <= 0f)
                {
                    SetVisible(false);
                    return;
                }
                
                if (OWInput.IsNewlyPressed(InputLibrary.cancel, InputMode.SatelliteCam))
                {
                    if (_controllingShip)
                    {
                        ExitShipViewerMode();
                    }
                    else
                    {
                        SetVisible(false);
                    }
                    return;
                }

                if (_controllingShip)
                {
                    _changeCameraPrompt.SetVisibility(true);
                    
                    bool changeCamera = OWInput.IsNewlyPressed(InputLibrary.left) ||
                        OWInput.IsNewlyPressed(InputLibrary.right);
                    
                    if (changeCamera)
                    {
                        CycleCamera(OWInput.IsNewlyPressed(InputLibrary.right));
                        Locator.GetMenuAudioController()._audioSource
                            .PlayOneShot(AudioType.Menu_LeftRight);
                    }

                    return;
                }
                
                _cycleCommandPrompt.SetVisibility(true);
                _cycleGroupPrompt.SetVisibility(true);

                if (_currentCommand.CanActivate())
                {
                    if (OWInput.IsNewlyPressed(InputLibrary.interact))
                    {
                        _currentCommand.Activate();
                        Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.ShipLogMoveBetweenEntries);

                        if (ShipEnhancements.InMultiplayer)
                        {
                            foreach (uint id in ShipEnhancements.PlayerIDs)
                            {
                                ShipEnhancements.QSBCompat.SendShipCommand(id, _currentCommand.GetType().Name);
                            }
                        }
                    }
                }

                bool changeGroup = OWInput.IsNewlyPressed(InputLibrary.left) ||
                    OWInput.IsNewlyPressed(InputLibrary.right);
                bool changeCommand = OWInput.IsNewlyPressed(InputLibrary.up) ||
                    OWInput.IsNewlyPressed(InputLibrary.down);

                if (changeGroup)
                {
                    CycleGroup(OWInput.IsNewlyPressed(InputLibrary.right));
                    _groupHeader.text = _groupToDisplayName[_groupMask];
                    _canvasList.SetCommands(_commandsByGroup[_groupMask].Where(c => c.CanShow()).ToList());
                }
                if (changeCommand)
                {
                    CycleCommand(OWInput.IsNewlyPressed(InputLibrary.down));
                }

                if (changeGroup || changeCommand)
                {
                    Locator.GetMenuAudioController()._audioSource
                        .PlayOneShot(changeGroup ? AudioType.Menu_LeftRight : AudioType.Menu_UpDown);
                }
            }
        }
        else if (_scopeEquipped && !OWInput.IsInputMode(InputMode.Menu))
        {
            Locator.GetPromptManager().RemoveScreenPrompt(_interactPrompt, PromptPosition.Center);
            Locator.GetPromptManager().RemoveScreenPrompt(_cycleCommandPrompt, PromptPosition.UpperRight);
            Locator.GetPromptManager().RemoveScreenPrompt(_cycleGroupPrompt, PromptPosition.UpperRight);
            Locator.GetPromptManager().RemoveScreenPrompt(_changeCameraPrompt, PromptPosition.UpperRight);

            if (_visible)
            {
                SetVisible(false);
            }
            
            _scopeEquipped = false;
        }
    }

    private void CycleCommand(bool forward)
    {
        int cycle = 0;
        var commands = _commandsByGroup[_groupMask];
        while (cycle < commands.Count)
        {
            int index = (commands.IndexOf(_currentCommand) + commands.Count + (forward ? 1 : -1)) % commands.Count;
            _currentCommand = commands[index];

            if (_currentCommand.CanShow())
            {
                _canvasList.SelectCommand(_currentCommand);
                break;
            }

            cycle++;
        }
    }

    private void RefreshCommand()
    {
        _currentCommand = _commandsByGroup[_groupMask][0];
        if (!_currentCommand.CanShow())
        {
            CycleCommand(true);
        }
        else
        {
            _canvasList.SelectCommand(_currentCommand);
        }
    }
    
    private void CycleGroup(bool forward)
    {
        int cycle = 0;
        var groups = _commandsByGroup.Keys.ToList();
        while (cycle < groups.Count)
        {
            int index = (groups.IndexOf(_currentCommand.GetCommandGroup()) + groups.Count + (forward ? 1 : -1)) % groups.Count;
            _groupMask = groups[index];

            if (_commandsByGroup[_groupMask].Any(c => c.CanShow()))
            {
                _currentCommand = _commandsByGroup[_groupMask][0];
                RefreshCommand();
                break;
            }

            cycle++;
        }
    }

    private void CycleCamera(bool forward)
    {
        int cycle = 0;
        while (cycle < _shipCameras.Count)
        {
            int index = (_shipCameraIndex + _shipCameras.Count + (forward ? 1 : -1)) % _shipCameras.Count;

            if (_shipCameras[index] != null)
            {
                _shipCameras[_shipCameraIndex].Disable();
                _shipCameraIndex = index;
                _shipCameras[_shipCameraIndex].Enable(_shipViewerTexture);
                
                break;
            }
        }
    }

    public void SetVisible(bool visible)
    {
        if (_visible != visible)
        {
            _visible = visible;

            if (_visible)
            {
                Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().LockOn(_shipAudioSignal.transform);
                _lastInputMode = OWInput.GetInputMode();
                OWInput.ChangeInputMode(InputMode.SatelliteCam);
                
                RefreshCommand();
                _groupHeader.text = _groupToDisplayName[_groupMask];
                _canvasList.SetCommands(_commandsByGroup[_groupMask].Where(c => c.CanShow()).ToList());
                Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.ShipLogSelectEntry);
                _animator.AnimateTo(1f, Vector3.one, 0.1f);
            }
            else
            {
                if (_controllingShip)
                {
                    ExitShipViewerMode();
                }
                
                Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().BreakLock();
                OWInput.ChangeInputMode(_lastInputMode);
                SetVisible(false);
                Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.ShipLogDeselectEntry);
                _animator.AnimateTo(0f, new Vector3(1f, 0f, 1f), 0.1f);
            }
        }
    }

    public void EnterShipViewerMode()
    {
        _canvasList.gameObject.SetActive(false);
        _shipViewerParent.SetActive(true);
        _groupHeader.text = "Ship Viewer";

        var detector = SELocator.GetShipDetector().GetComponent<SectorDetector>();
        var tempList = new List<Sector>();
        tempList.AddRange(detector._sectorList);
        for (int i = detector._sectorList.Count - 1; i >= 0; i--)
        {
            detector.RemoveSector(detector._sectorList[i]);
        }
        
        detector.SetOccupantType(DynamicOccupant.Probe);
        foreach (var sector in tempList)
        {
            detector.AddSector(sector);
        }
        
        _viewerImage.material.SetTexture("_MainTex", _shipViewerTexture);
        _viewerImage.SetMaterialDirty();

        _shipCameras[_shipCameraIndex].Enable(_shipViewerTexture);

        Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.ShipLogSelectPlanet);

        _controllingShip = true;
    }

    public void ExitShipViewerMode()
    {
        _shipCameras[_shipCameraIndex].Disable();
        _viewerImage.material.SetTexture("_MainTex", null);
        _viewerImage.SetMaterialDirty();
        
        var detector = SELocator.GetShipDetector().GetComponent<SectorDetector>();
        var tempList = new List<Sector>();
        tempList.AddRange(detector._sectorList);
        for (int i = detector._sectorList.Count - 1; i >= 0; i--)
        {
            detector.RemoveSector(detector._sectorList[i]);
        }
        
        detector.SetOccupantType(DynamicOccupant.Ship);
        foreach (var sector in tempList)
        {
            detector.AddSector(sector);
        }
        
        _shipViewerParent.SetActive(false);
        _canvasList.gameObject.SetActive(true);
        _groupHeader.text = _groupToDisplayName[_groupMask];
        
        Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.ShipLogDeselectPlanet);

        _controllingShip = false;
    }

    public void ReceiveCommandRemote(string commandName)
    {
        ShipCommand command = null;
        foreach (var cmd in _commands)
        {
            if (cmd.GetType().Name == commandName)
            {
                command = cmd;
            }
        }

        if (command == null) return;
        
        if (command.CanActivate())
        {
            command.ActivateRemote();
        }
    }

    public bool IsVisible() => _visible;

    private void OnShipSystemFailure()
    {
        if (_visible)
        {
            SetVisible(false);
        }
    }

    private void OnDestroy()
    {
        _shipViewerTexture = null;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}