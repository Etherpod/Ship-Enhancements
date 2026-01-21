using System;
using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class QSBCompatibility
{
    private readonly IQSBAPI _api;
    private List<CockpitSwitch> _activeSwitches = [];
    private ShipEngineSwitch _engineSwitch;
    private List<TetherHookItem> _activeTetherHooks = [];
    private SettingsPresets.PresetName _hostPreset = SettingsPresets.PresetName.Custom;
    private bool _neverInitialized = true;

    [Serializable]
    private struct NoData { }

    [Serializable]
    private struct SerializedVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializedVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 Vector
        { 
            get 
            { 
                return new Vector3(x, y, z); 
            } 
            set 
            { 
                x = value.x;
                y = value.y;
                z = value.z;
            } 
        }
    }

    public QSBCompatibility(IQSBAPI api)
    {
        _api = api;
        _api.OnPlayerJoin().AddListener(OnPlayerJoin);
        _api.RegisterHandler<(string, object)>("settings-data", ReceiveSettingsData);
        _api.RegisterHandler<int>("host-preset", ReceiveHostPreset);
        _api.RegisterHandler<(int, bool)>("switch-state", ReceiveSwitchState);
        _api.RegisterHandler<(int, bool, bool, bool)>("button-state", ReceiveButtonState);
        _api.RegisterHandler<(int, bool, bool, bool, bool)>("button-switch-state", ReceiveButtonSwitchState);
        _api.RegisterHandler<NoData>("ship-initialized", ReceiveInitializedShip);
        _api.RegisterHandler<NoData>("world-objects-ready", ReceiveWorldObjectsInitialized);
        _api.RegisterHandler<(bool, bool)>("engine-switch-state", ReceiveEngineSwitchState);
        _api.RegisterHandler<bool>("initialize-engine-switch", InitializeEngineSwitch);
        _api.RegisterHandler<(float, bool)>("ship-oxygen-drain", ReceiveShipOxygenDrain);
        _api.RegisterHandler<(float, bool)>("ship-fuel-drain", ReceiveShipFuelDrain);
        _api.RegisterHandler<float>("set-ship-oxygen", ReceiveShipOxygenValue);
        _api.RegisterHandler<float>("set-ship-fuel", ReceiveShipFuelValue);
        _api.RegisterHandler<float>("set-ship-water", ReceiveShipWaterValue);
        _api.RegisterHandler<bool>("panel-state", ReceivePanelExtended);
        _api.RegisterHandler<(int, bool)>("modulator-button-state", ReceiveModulatorButtonState);
        _api.RegisterHandler<(bool, bool, bool)>("overdrive-button-state", ReceiveOverdriveButtonState);
        _api.RegisterHandler<NoData>("overdrive-stop-coroutines", ReceiveStopOverdriveCoroutines);
        _api.RegisterHandler<int>("campfire-reactor-damage", ReceiveCampfireReactorDamaged);
        _api.RegisterHandler<int>("campfire-extinguished", ReceiveCampfireExtinguished);
        _api.RegisterHandler<(int, bool, bool, bool)>("campfire-initial-state", ReceiveCampfireInitialState);
        _api.RegisterHandler<float>("ship-temp-meter", ReceiveShipHullTemp);
        _api.RegisterHandler<int>("attach-tether", ReceiveAttachTether);
        _api.RegisterHandler<int>("disconnect-tether", ReceiveDisconnectTether);
        _api.RegisterHandler<(int, int)>("transfer-tether", ReceiveTransferTether);
        _api.RegisterHandler<NoData>("ship-fuel-max", ReceiveShipFuelMax);
        _api.RegisterHandler<(int, float, float, int, float, float, float)>("initial-cockpit-effect-state", ReceiveInitialCockpitEffectState);
        _api.RegisterHandler<(float, float)>("current-cockpit-effect-state", ReceiveCurrentCockpitEffectState);
        _api.RegisterHandler<SerializedVector3>("detach-all-players", ReceiveDetachAllPlayers);
        _api.RegisterHandler<SerializedVector3>("persistent-input", ReceivePersistentInput);
        _api.RegisterHandler<float>("initial-black-hole", ReceiveInitialBlackHoleState);
        _api.RegisterHandler<ShipCommand>("send-ship-command", ReceiveShipCommand);
        _api.RegisterHandler<(bool, string, SerializedVector3)>("activate-warp", ReceiveActivateWarp);
        _api.RegisterHandler<(int, bool)>("toggle-fuel-tank-drain", ReceiveToggleFuelTankDrain);
        _api.RegisterHandler<int>("fuel-tank-explosion", ReceiveFuelTankExplosion);
        _api.RegisterHandler<(int, float)>("fuel-tank-capacity", ReceiveFuelTankCapacity);
        _api.RegisterHandler<(int, int)>("item-module-parent", ReceiveItemModuleParent);
        _api.RegisterHandler<(int, bool)>("tractor-beam-turbo", ReceiveTractorBeamTurbo);
        _api.RegisterHandler<bool>("set-curtain-state", ReceiveCurtainState);
        _api.RegisterHandler<string>("send-ernesto-comment", ReceiveErnestoComment);
        _api.RegisterHandler<float>("detach-landing-gear", ReceiveDetachLandingGear);
        _api.RegisterHandler<(int, bool)>("radio-power", ReceiveRadioPower);
        _api.RegisterHandler<(int, int[])>("radio-codes", ReceiveRadioCodes);
        _api.RegisterHandler<int>("radio-cancel-tuning", ReceiveRadioCancelTuning);
        _api.RegisterHandler<(int, float)>("radio-volume", ReceiveRadioVolume);
        _api.RegisterHandler<(int, int, bool)>("create-item", ReceiveCreateItem);
        _api.RegisterHandler<bool>("grav-gear-invert", ReceiveGravInvertSwitchState);
        _api.RegisterHandler<int>("angler-death", ReceiveAnglerDeath);
        _api.RegisterHandler<(int, bool, bool, bool, bool)>("autopilot-state", ReceiveAutopilotState);
        _api.RegisterHandler<(bool, bool, bool)>("pid-autopilot-state", ReceivePidAutopilotState);
        _api.RegisterHandler<NoData>("honk-horn", ReceiveHonkHorn);
        _api.RegisterHandler<(int, int)>("pump-type", ReceivePumpType);
        _api.RegisterHandler<(int, bool)>("pump-powered", ReceivePumpPowered);
        _api.RegisterHandler<(int, bool)>("pump-mode", ReceivePumpMode);
        _api.RegisterHandler<bool>("water-cooling", ReceiveWaterCoolingState);
        _api.RegisterHandler<bool>("reactor-overload", ReceiveReactorOverload);
    }

    private void OnPlayerJoin(uint playerID)
    {
        if (!_api.GetIsHost() || _api.GetLocalPlayerID() == playerID)
        {
            return;
        }

        _neverInitialized = true;

        SendSettingsData(playerID);
        SendHostPreset(playerID);
    }

    #region Settings
    public void SendSettingsData(uint id)
    {
        var allSettings = Enum.GetValues(typeof(ShipEnhancements.Settings)) as ShipEnhancements.Settings[];
        foreach (var setting in allSettings)
        {
            _api.SendMessage("settings-data", (setting.GetName(), setting.GetProperty()), id, false);
        }
    }

    private void ReceiveSettingsData(uint id, (string settingName, object settingValue) data)
    {
        var allSettings = Enum.GetValues(typeof(ShipEnhancements.Settings)) as ShipEnhancements.Settings[];
        foreach (var setting in allSettings)
        {
            if (setting.GetName() == data.settingName)
            {
                setting.SetProperty(data.settingValue);
                return;
            }
        }
        ShipEnhancements.WriteDebugMessage($"Setting {data.settingName} not found", error: true);
    }

    public void SendHostPreset(uint id)
    {
        _api.SendMessage("host-preset", (int)ShipEnhancements.Instance.GetCurrentPreset(), id, false);
    }

    private void ReceiveHostPreset(uint id, int preset)
    {
        _hostPreset = (SettingsPresets.PresetName)preset;
    }

    public SettingsPresets.PresetName GetHostPreset()
    {
        return _hostPreset;
    }
    #endregion

    #region Initialization
    public void SendInitializedShip(uint id)
    {
        _neverInitialized = false;
        _api.SendMessage("ship-initialized", new NoData(), id, false);
        ShipEnhancements.Instance.ModHelper.Events.Unity.RunWhen(
            ShipEnhancements.QSBInteraction.WorldObjectsLoaded, () =>
            {
                _api.SendMessage("world-objects-ready", new NoData(), id, false);
            });
    }

    private void ReceiveInitializedShip(uint id, NoData noData)
    {
        if (!_api.GetIsHost())
        {
            return;
        }

        if (_engineSwitch != null)
        {
            _api.SendMessage("initialize-engine-switch", ShipEnhancements.Instance.engineOn, id, false);
        }
        if ((bool)addPortableCampfire.GetProperty())
        {
            foreach (PortableCampfireItem campfire in UnityEngine.Object.FindObjectsOfType<PortableCampfireItem>())
            {
                bool dropped = false;
                bool unpacked = false;
                bool lit = false;

                if (campfire.IsDropped())
                {
                    dropped = true;
                    if (campfire.IsUnpacked())
                    {
                        unpacked = true;
                        if (!campfire.GetCampfire().IsExtinguished())
                        {
                            lit = true;
                        }
                    }
                }
                SendCampfireInitialState(id, campfire, dropped, unpacked, lit);
            }
        }
        if ((float)rustLevel.GetProperty() > 0 || ((float)dirtAccumulationTime.GetProperty() > 0f 
            && (float)maxDirtAccumulation.GetProperty() > 0f))
        {
            SELocator.GetCockpitFilthController()?.BroadcastCockpitEffectState();
        }
        if ((float)shipExplosionMultiplier.GetProperty() < 0f)
        {
            BlackHoleExplosionController controller = SELocator.GetShipTransform().GetComponentInChildren<BlackHoleExplosionController>();
            if (controller != null && controller.IsPlaying())
            {
                SendInitialBlackHoleState(id, controller.GetCurrentScale());
            }
        }
        if ((bool)unlimitedItems.GetProperty())
        {
            foreach (SEItemSocket socket in UnityEngine.Object.FindObjectsOfType<SEItemSocket>())
            {
                OWItem[] spawned = socket.GetSpawnedItems();
                for (int i = 0; i < spawned.Length; i++)
                {
                    OWItem item = spawned[i];
                    SendCreateItem(id, item, socket, item.GetComponentInParent<SEItemSocket>() == socket);
                }
            }
        }
    }

    private void ReceiveWorldObjectsInitialized(uint joiningID, NoData noData)
    {
        ShipEnhancements.Instance.ModHelper.Events.Unity.FireInNUpdates(() =>
        {
            foreach (var hook in _activeTetherHooks)
            {
                var tether = hook.GetTether();
                if (tether.IsTethered())
                {
                    if (tether.GetConnectedBody() == Locator.GetPlayerBody())
                    {
                        SendAttachTether(joiningID, hook);
                    }
                }
            }

            if (!_api.GetIsHost())
            {
                return;
            }

            foreach (var hook in _activeTetherHooks)
            {
                if (hook.GetTether() != hook.GetActiveTether())
                {
                    SendTransferTether(joiningID, hook, hook.GetActiveTether().GetHook());
                }
            }
        }, 2);
    }

    public bool NeverInitialized()
    {
        return _neverInitialized;
    }
    #endregion

    #region Switches
    public void SendSwitchState(uint id, CockpitSwitch cockpitSwitch, bool state)
    {
        _api.SendMessage("switch-state", (ShipEnhancements.QSBInteraction.GetIDFromSwitch(cockpitSwitch), state), id, false);
    }

    private void ReceiveSwitchState(uint id, (int switchID, bool state) data)
    {
        CockpitSwitch s = ShipEnhancements.QSBInteraction.GetSwitchFromID(data.switchID);
        if (s != null)
        {
            s.SetState(data.state);
        }
    }

    public void SendButtonState(uint id, CockpitButton button, bool state, bool doEvent = true, bool doAction = true)
    {
        _api.SendMessage("button-state", (ShipEnhancements.QSBInteraction.GetIDFromButton(button), state, doEvent, doAction), id, false);
    }

    private void ReceiveButtonState(uint id, (int buttonID, bool state, bool doEvent, bool doAction) data)
    {
        CockpitButton b = ShipEnhancements.QSBInteraction.GetButtonFromID(data.buttonID);
        if (b != null)
        {
            bool lastState = b.IsOn();
            b.SetState(data.state);
            if (lastState != data.state)
            {
                if (data.doEvent)
                {
                    b.RaiseChangeStateEvent();
                }
                if (data.doAction)
                {
                    b.OnChangeStateEvent();
                }
            }
        }
    }

    public void SendButtonSwitchState(uint id, CockpitButtonSwitch buttonSwitch, bool state, bool activated, bool doEvent = true, bool doAction = true)
    {
        _api.SendMessage("button-switch-state", (ShipEnhancements.QSBInteraction.GetIDFromButton(buttonSwitch), state, activated,
            doEvent, doAction), id, false);
    }

    private void ReceiveButtonSwitchState(uint id, (int bsID, bool state, bool activated, bool doEvent, bool doAction) data)
    {
        CockpitButtonSwitch bs = ShipEnhancements.QSBInteraction.GetButtonSwitchFromID(data.bsID);
        if (bs != null)
        {
            bool lastState = bs.IsOn();
            bool lastActive = bs.IsActivated();
            bs.SetState(data.state);
            bs.SetActive(data.activated);
            if (lastState != data.state)
            {
                if (data.doEvent)
                {
                    bs.RaiseChangeStateEvent();
                }
                if (data.doAction)
                {
                    bs.OnChangeStateEvent();
                }
            }
            if (lastActive != data.activated)
            {
                if (data.doEvent)
                {
                    bs.RaiseChangeActiveEvent();
                }
                if (data.doAction)
                {
                    bs.OnChangeActiveEvent();
                }
            }
        }
    }
    #endregion

    #region Engine Switch
    public void SetEngineSwitch(ShipEngineSwitch engineSwitch)
    {
        _engineSwitch = engineSwitch;
    }

    public void RemoveEngineSwitch()
    {
        _engineSwitch = null;
    }

    public void SendEngineSwitchState(uint id, bool wasPressed, bool turnOff)
    {
        _api.SendMessage("engine-switch-state", (wasPressed, turnOff), id, false);
    }

    private void ReceiveEngineSwitchState(uint id, (bool wasPressed, bool turnOff) data)
    {
        _engineSwitch?.UpdateWasPressed(data.wasPressed, data.turnOff);
    }

    private void InitializeEngineSwitch(uint id, bool completedIgnition)
    {
        _engineSwitch.InitializeEngineSwitch(completedIgnition);
    }
    #endregion

    #region Resource Sync
    public void SendShipOxygenDrain(uint id, float drainAmount, bool applyMultipliers)
    {
        _api.SendMessage("ship-oxygen-drain", (drainAmount, applyMultipliers), id, false);
    }

    private void ReceiveShipOxygenDrain(uint id, (float, bool) data)
    {
        if (SELocator.GetShipResources() == null) return;

        if (data.Item1 >= 0)
        {
            if (data.Item2)
            {
                SELocator.GetShipResources().DrainOxygen(data.Item1);
            }
            else
            {
                SELocator.GetShipResources()._currentOxygen -= data.Item1;
            }
        }
        else
        {
            SELocator.GetShipResources().AddOxygen(-data.Item1);
        }
    }

    public void SendShipFuelDrain(uint id, float drainAmount, bool applyMultipliers)
    {
        _api.SendMessage("ship-fuel-drain", (drainAmount, applyMultipliers), id, false);
    }

    private void ReceiveShipFuelDrain(uint id, (float, bool) data)
    {
        if (SELocator.GetShipResources() == null) return;

        if (data.Item1 >= 0)
        {
            if (data.Item2)
            {
                SELocator.GetShipResources().DrainFuel(data.Item1);
            }
            else
            {
                SELocator.GetShipResources()._currentFuel -= data.Item1;
            }
        }
        else
        {
            SELocator.GetShipResources().AddFuel(-data.Item1);
        }
    }

    public void SendShipOxygenValue(uint id, float newValue)
    {
        _api.SendMessage("set-ship-oxygen", newValue, id, false);
    }

    private void ReceiveShipOxygenValue(uint id, float newValue)
    {
        SELocator.GetShipResources()?.SetOxygen(newValue);
    }

    public void SendShipFuelValue(uint id, float newValue)
    {
        _api.SendMessage("set-ship-fuel", newValue, id, false);
    }

    private void ReceiveShipFuelValue(uint id, float newValue)
    {
        SELocator.GetShipResources()?.SetFuel(newValue);
    }

    public void SendShipWaterValue(uint id, float newValue)
    {
        _api.SendMessage("set-ship-water", newValue, id, false);
    }

    private void ReceiveShipWaterValue(uint id, float newValue)
    {
        SELocator.GetShipWaterResource()?.SetWater(newValue);
    }

    public void SendShipFuelMax(uint id)
    {
        _api.SendMessage("ship-fuel-max", new NoData(), id, false);
    }

    private void ReceiveShipFuelMax(uint id, NoData noData)
    {
        SELocator.GetShipTransform().GetComponentInChildren<ShipFuelTransfer>()?.UpdateInteractable();
    }
    #endregion

    #region Button Panel
    public void SendPanelExtended(uint id, bool extended)
    {
        _api.SendMessage("panel-state", extended, id, false);
    }

    private void ReceivePanelExtended(uint id, bool extended)
    {
        SELocator.GetButtonPanel()?.UpdateExtended(extended);
    }
    #endregion

    #region Thrust Modulator
    public void SendModulatorButtonState(uint id, int level, bool pressed)
    {
        _api.SendMessage("modulator-button-state", (level, pressed), id, false);
    }

    private void ReceiveModulatorButtonState(uint id, (int, bool) data)
    {
        ThrustModulatorButton button = SELocator.GetThrustModulatorController().GetModulatorButton(data.Item1);

        if (!button) return;

        if (data.Item2)
        {
            button.PressButton();
        }
        else
        {
            button.ReleaseButton();
        }
    }

    public void SendOverdriveButtonState(uint id, bool isPrimeButton, bool pressed, bool reset = false)
    {
        _api.SendMessage("overdrive-button-state", (isPrimeButton, pressed, reset), id, false);
    }

    private void ReceiveOverdriveButtonState(uint id, (bool, bool, bool) data)
    {
        OverdriveButton button = SELocator.GetShipOverdriveController().GetButton(data.Item1);

        if (data.Item3)
        {
            if (!SELocator.GetShipDamageController().IsElectricalFailed()
                && !ShipEnhancements.Instance.fuelDepleted)
            {
                if (data.Item1)
                {
                    button.SetButtonOn(false);
                }
                else
                {
                    button.SetButtonActive(false);
                }
            }

            return;
        }

        if (data.Item2)
        {
            button.PressButton();
        }
        else
        {
            button.ReleaseButton();
        }
    }

    public void SendStopOverdriveCoroutines(uint id)
    {
        _api.SendMessage("overdrive-stop-coroutines", new NoData(), id, false);
    }

    private void ReceiveStopOverdriveCoroutines(uint id, NoData noData)
    {
        SELocator.GetShipOverdriveController()?.StopAllCoroutines();
    }
    #endregion

    #region Portable Campfire
    public void SendCampfireReactorDamaged(uint id, OWItem item)
    {
        _api.SendMessage("campfire-reactor-delay", ShipEnhancements.QSBInteraction.GetIDFromItem(item), id, false);
    }

    private void ReceiveCampfireReactorDamaged(uint id, int itemID)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(itemID);
        if (item == null) return;

        PortableCampfireItem campfire = item as PortableCampfireItem;
        campfire.GetCampfire().OnRemoteReactorDamaged();
    }

    public void SendCampfireExtinguishState(uint id, OWItem item)
    {
        _api.SendMessage("campfire-extinguished", ShipEnhancements.QSBInteraction.GetIDFromItem(item), id, false);
    }

    private void ReceiveCampfireExtinguished(uint id, int itemID)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(itemID);
        if (item == null) return;

        PortableCampfireItem campfire = item as PortableCampfireItem;
        campfire.GetCampfire().OnExtinguishInteract();
    }

    public void SendCampfireInitialState(uint id, OWItem item, bool dropped, bool unpacked, bool lit)
    {
        _api.SendMessage("campfire-initial-state", (ShipEnhancements.QSBInteraction.GetIDFromItem(item), dropped, unpacked, lit), id, false);
    }

    private void ReceiveCampfireInitialState(uint id, (int itemID, bool dropped, bool unpacked, bool lit) data)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        if (item == null) return;

        PortableCampfireItem campfire = item as PortableCampfireItem;
        if (!campfire.IsDropped()) return;

        if (data.dropped)
        {
            campfire.TogglePackUp(false);

            if (data.unpacked)
            {
                // not using the lit data?
                campfire.GetCampfire().SetInitialState(Campfire.State.LIT);
                campfire.GetCampfire().SetState(Campfire.State.LIT);
            }
        }
    }
    #endregion

    #region Temperature
    public void SendShipHullTemp(uint id, float temperature)
    {
        _api.SendMessage("ship-temp-meter", temperature, id, false);
    }

    private void ReceiveShipHullTemp(uint id, float temperature)
    {
        SELocator.GetShipTemperatureDetector()?.SetInternalTempRemote(temperature);
    }
    #endregion

    #region Tether
    public void SendAttachTether(uint id, TetherHookItem hook)
    {
        _api.SendMessage("attach-tether", ShipEnhancements.QSBInteraction.GetIDFromItem(hook), id, false);
    }

    private void ReceiveAttachTether(uint id, int hookID)
    {
        if (!ShipEnhancements.QSBInteraction.WorldObjectsLoaded()) return;
        ((TetherHookItem)ShipEnhancements.QSBInteraction.GetItemFromID(hookID)).OnConnectTetherRemote(id);
    }

    public void SendDisconnectTether(uint id, TetherHookItem hook)
    {
        _api.SendMessage("disconnect-tether", ShipEnhancements.QSBInteraction.GetIDFromItem(hook), id, false);
    }

    private void ReceiveDisconnectTether(uint id, int hookID)
    {
        if (!ShipEnhancements.QSBInteraction.WorldObjectsLoaded()) return;
        ((TetherHookItem)ShipEnhancements.QSBInteraction.GetItemFromID(hookID)).OnDisconnectTetherRemote();
    }

    public void SendTransferTether(uint id, TetherHookItem newHook, TetherHookItem lastHook)
    {
        int newID = ShipEnhancements.QSBInteraction.GetIDFromItem(newHook);
        int lastID = ShipEnhancements.QSBInteraction.GetIDFromItem(lastHook);
        _api.SendMessage("transfer-tether", (newID, lastID), id, false);
    }

    private void ReceiveTransferTether(uint id, (int newID, int lastID) data)
    {
        if (!ShipEnhancements.QSBInteraction.WorldObjectsLoaded()) return;
        Tether newTether = ((TetherHookItem)ShipEnhancements.QSBInteraction.GetItemFromID(data.lastID)).GetTether();
        ((TetherHookItem)ShipEnhancements.QSBInteraction.GetItemFromID(data.newID)).OnTransferRemote(newTether);
    }

    public void AddTetherHook(TetherHookItem hook)
    {
        _activeTetherHooks.Add(hook);
    }

    public void RemoveTetherHook(TetherHookItem hook)
    {
        if (_activeTetherHooks.Contains(hook))
        {
            _activeTetherHooks.Remove(hook);
        }
    }
    #endregion

    #region CockpitFlith

    public void SendInitialCockpitEffectState(uint id, int rustIndex, Vector2 rustOffset, int dirtIndex, Vector2 dirtOffset, float dirtProgression)
    {
        _api.SendMessage("initial-cockpit-effect-state", (rustIndex, rustOffset.x, rustOffset.y, dirtIndex, dirtOffset.x, dirtOffset.y, dirtProgression), id, false);
    }

    private void ReceiveInitialCockpitEffectState(uint id, (int rustIndex, float rustOffsetX, float rustOffsetY, 
        int dirtIndex, float dirtOffsetX, float dirtOffsetY, float dirtProgression) data)
    {
        SELocator.GetCockpitFilthController()?.SetInitialEffectState(data.rustIndex, new Vector2(data.rustOffsetX, data.rustOffsetY),
            data.dirtIndex, new Vector2(data.dirtOffsetX, data.dirtOffsetY), data.dirtProgression);
    }

    public void SendCurrentCockpitEffectState(uint id, float dirtProgression, float iceBuildup)
    {
        _api.SendMessage("current-cockpit-effect-state", (dirtProgression, iceBuildup), id, false);
    }

    private void ReceiveCurrentCockpitEffectState(uint id, (float dirtProgression, float iceBuildup) data)
    {
        SELocator.GetCockpitFilthController()?.UpdateEffectState(data.dirtProgression, data.iceBuildup);
    }

    #endregion

    #region DisableSeatbelt
    public void SendDetachAllPlayers(uint id, Vector3 velocity)
    {
        _api.SendMessage("detach-all-players", new SerializedVector3(velocity), id, false);
    }

    private void ReceiveDetachAllPlayers(uint id, SerializedVector3 velocity)
    {
        ShipEnhancements.QSBInteraction.OnDetachAllPlayers(velocity.Vector);
    }
    #endregion

    #region BlackHoleExplosion
    public void SendInitialBlackHoleState(uint id, float scale)
    {
        _api.SendMessage("initial-black-hole", scale, id, false);
    }

    private void ReceiveInitialBlackHoleState(uint id, float scale)
    {
        BlackHoleExplosionController controller = SELocator.GetShipTransform().GetComponentInChildren<BlackHoleExplosionController>();
        controller?.SetInitialBlackHoleState(scale);
    }
    #endregion

    #region ShipCommands
    public void SendShipCommand(uint id, ShipCommand command)
    {
        _api.SendMessage("send-ship-command", command, id, false);
    }

    private void ReceiveShipCommand(uint id, ShipCommand command)
    {
        SELocator.GetPlayerBody().GetComponentInChildren<ShipRemoteControl>()?.ReceiveCommandRemote(command);
    }
    #endregion

    #region ShipWarpCore
    public void SendActivateWarp(uint id, bool playerInShip, string targetCannonEntryID, Vector3 randomPos)
    {
        _api.SendMessage("activate-warp", (playerInShip, targetCannonEntryID, new SerializedVector3(randomPos)), id, false);
    }

    private void ReceiveActivateWarp(uint id, (bool playerInShip, string targetCannonEntryID, SerializedVector3 randomPos) data)
    {
        SELocator.GetShipTransform().GetComponentInChildren<ShipWarpCoreController>()?
            .ActivateWarpRemote(data.playerInShip, data.targetCannonEntryID, data.randomPos.Vector);
    }
    #endregion

    #region FuelTankItem
    public void SendToggleFuelTankDrain(uint id, OWItem item, bool started)
    {
        _api.SendMessage("toggle-fuel-tank-drain", (ShipEnhancements.QSBInteraction.GetIDFromItem(item), started), id, false);
    }

    private void ReceiveToggleFuelTankDrain(uint id, (int itemID, bool started) data)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        if (item == null) return;

        FuelTankItem fuelTank = item as FuelTankItem;
        fuelTank.ToggleDrainRemote(data.started);
    }

    public void SendFuelTankExplosion(uint id, OWItem item)
    {
        _api.SendMessage("fuel-tank-explosion", ShipEnhancements.QSBInteraction.GetIDFromItem(item), id, false);
    }

    private void ReceiveFuelTankExplosion(uint id, int itemID)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(itemID);
        if (item == null) return;

        FuelTankItem fuelTank = item as FuelTankItem;
        fuelTank.ExplodeRemote();
    }

    public void SendFuelTankCapacity(uint id, OWItem item, float fuel)
    {
        _api.SendMessage("fuel-tank-capacity", (ShipEnhancements.QSBInteraction.GetIDFromItem(item), fuel), id, false);
    }

    private void ReceiveFuelTankCapacity(uint id, (int itemID, float fuel) data)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        if (item == null) return;

        FuelTankItem fuelTank = item as FuelTankItem;
        fuelTank.UpdateFuelRemote(data.fuel);
    }
    #endregion

    #region ShipItemPlacement
    public void SendItemModuleParent(uint id, OWItem item, int shipModulesIndex)
    {
        _api.SendMessage("item-module-parent", (ShipEnhancements.QSBInteraction.GetIDFromItem(item), shipModulesIndex), id, false);
    }

    private void ReceiveItemModuleParent(uint id, (int itemID, int shipModulesIndex) data)
    {
        Transform parent = null;
        if (data.shipModulesIndex >= 100)
        {
            parent = SELocator.GetShipTransform().GetComponentInChildren<ShipLandingGear>()
                .GetLegs()[data.shipModulesIndex - 100].transform;
        }
        else
        {
            parent = SELocator.GetShipDamageController()._shipModules[data.shipModulesIndex].transform;
        }
        
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        item.transform.parent = parent;
    }
    #endregion

    #region PortableTractorBeam
    public void SendTractorBeamTurbo(uint id, PortableTractorBeamItem item, bool enableTurbo)
    {
        _api.SendMessage("tractor-beam-turbo", (ShipEnhancements.QSBInteraction.GetIDFromItem(item), enableTurbo), id, false);
    }

    private void ReceiveTractorBeamTurbo(uint id, (int itemID, bool enableTurbo) data)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        if (item == null) return;

        PortableTractorBeamItem tractorBeam = item as PortableTractorBeamItem;
        tractorBeam.ToggleTurbo(data.enableTurbo);
    }
    #endregion

    #region Curtains
    public void SendCurtainState(uint id, bool open)
    {
        _api.SendMessage("set-curtain-state", open, id, false);
    }

    public void ReceiveCurtainState(uint id, bool open)
    {
        CockpitCurtainController curtains = SELocator.GetShipTransform().GetComponentInChildren<CockpitCurtainController>();
        if (curtains != null)
        {
            curtains.UpdateCurtainRemote(open);
        }
    }
    #endregion

    #region Ernesto
    public void SendErnestoComment(uint id, string comment)
    {
        _api.SendMessage("send-ernesto-comment", comment, id, false);
    }

    private void ReceiveErnestoComment(uint id, string comment)
    {
        SELocator.GetErnesto()?.MakeCommentRemote(comment);
    }
    #endregion

    #region EjectLandingGear
    public void SendDetachLandingGear(uint id, float ejectImpulse)
    {
        _api.SendMessage("detach-landing-gear", ejectImpulse, id, false);
    }

    private void ReceiveDetachLandingGear(uint id, float ejectImpulse)
    {
        ShipLandingGear landingGear = SELocator.GetShipTransform().GetComponentInChildren<ShipLandingGear>();
        List<OWRigidbody> legs = [];
        foreach (ShipDetachableLeg leg in landingGear.GetLegs())
        {
            legs.Add(leg.Detach());
        }

        //_shipBody.transform.position -= _shipBody.transform.TransformVector(_ejectDirection);
        float num = ejectImpulse;
        if (Locator.GetShipDetector().GetComponent<ShipFluidDetector>().InOceanBarrierZone())
        {
            MonoBehaviour.print("Ship in ocean barrier zone, reducing eject impulse.");
            num = 1f;
        }
        SELocator.GetShipBody().AddLocalImpulse(Vector3.up * num / 2f);
        foreach (OWRigidbody leg in legs)
        {
            Vector3 toShip = leg.transform.position - SELocator.GetShipTransform().position;
            leg.AddLocalImpulse(-toShip.normalized * num);
        }
    }
    #endregion

    #region Radio
    public void SendRadioPower(uint id, OWItem item, bool powered)
    {
        _api.SendMessage("radio-power", (ShipEnhancements.QSBInteraction.GetIDFromItem(item), powered), id, false);
    }

    private void ReceiveRadioPower(uint id, (int itemID, bool powered) data)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        if (item == null) return;

        RadioItem radio = item as RadioItem;
        radio.SetRadioPowerRemote(data.powered);
    }

    public void SendRadioCodes(uint id, OWItem item, int[] codes)
    {
        _api.SendMessage("radio-codes", (ShipEnhancements.QSBInteraction.GetIDFromItem(item), codes), id, false);
    }

    private void ReceiveRadioCodes(uint id, (int itemID, int[] codes) data)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        if (item == null) return;

        RadioItem radio = item as RadioItem;
        radio.SetRadioCodesRemote(data.codes);
    }

    public void SendRadioCancelTuning(uint id, OWItem item)
    {
        _api.SendMessage("radio-cancel-tuning", ShipEnhancements.QSBInteraction.GetIDFromItem(item), id, false);
    }

    private void ReceiveRadioCancelTuning(uint id, int itemID)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(itemID);
        if (item == null) return;

        RadioItem radio = item as RadioItem;
        radio.CancelTuningRemote();
    }

    public void SendRadioVolume(uint id, OWItem item, float volume)
    {
        _api.SendMessage("radio-volume", (ShipEnhancements.QSBInteraction.GetIDFromItem(item), volume), id, false);
    }

    private void ReceiveRadioVolume(uint id, (int itemID, float volume) data)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        if (item == null) return;

        RadioItem radio = item as RadioItem;
        radio.ChangeVolumeRemote(data.volume);
    }
    #endregion

    #region ItemDuping
    public void SendCreateItem(uint id, OWItem item, OWItemSocket socket, bool socketItem = true)
    {
        _api.SendMessage("create-item", 
            (ShipEnhancements.QSBInteraction.GetIDFromItem(item), 
            ShipEnhancements.QSBInteraction.GetIDFromSocket(socket), socketItem), id, false);
    }

    private void ReceiveCreateItem(uint id, (int itemID, int socketID, bool socketItem) data)
    {
        if (!ShipEnhancements.QSBInteraction.WorldObjectsLoaded())
        {
            ShipEnhancements.Instance.ModHelper.Events.Unity.RunWhen(
                ShipEnhancements.QSBInteraction.WorldObjectsLoaded,
                () => ReceiveCreateItem(id, data));
            return;
        }

        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        if (item == null) return;

        OWItemSocket socket = ShipEnhancements.QSBInteraction.GetSocketFromID(data.socketID);
        if (socket == null) return;

        SEItemSocket itemSocket = socket as SEItemSocket;
        itemSocket.CreateItemRemote(item, data.socketItem);
    }
    #endregion

    #region GravInvertSwitch
    public void SendGravInvertSwitchState(uint id, bool enabled)
    {
        _api.SendMessage("grav-gear-invert", enabled, id, false);
    }

    private void ReceiveGravInvertSwitchState(uint id, bool enabled)
    {
        SELocator.GetShipTransform().GetComponentInChildren<GravityGearInvertSwitch>()?.SetState(enabled);
    }
    #endregion

    #region ExplosionDamage
    public void SendAnglerDeath(uint id, AnglerfishController angler)
    {
        _api.SendMessage("angler-death", ShipEnhancements.QSBInteraction.GetIDFromAngler(angler), id, false);
    }

    private void ReceiveAnglerDeath(uint id, int anglerID)
    {
        AnglerfishController angler = ShipEnhancements.QSBInteraction.GetAnglerFromID(anglerID);
        if (angler != null)
        {
            angler.ChangeState(AnglerfishController.AnglerState.Stunned);
            angler.GetComponentInChildren<AnglerfishFluidVolume>().SetVolumeActivation(false);
        }

        if (ShipEnhancements.AchievementsAPI != null && !SEAchievementTracker.AnglerfishKill)
        {
            SEAchievementTracker.AnglerfishKill = true;
            ShipEnhancements.AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.ANGLERFISH_KILL");
        }
    }
    #endregion

    #region Autopilot
    public void SendAutopilotState(uint id, OWRigidbody targetBody, bool destination = false, bool startMatch = false, bool stopMatch = false, bool abort = false)
    {
        _api.SendMessage("autopilot-state", 
            (targetBody != null ? ShipEnhancements.QSBInteraction.GetIDFromBody(targetBody) : -1,
            destination, startMatch, stopMatch, abort), id, false);
    }

    private void ReceiveAutopilotState(uint id, (int targetBodyID, bool destination, bool startMatch, bool stopMatch, bool abort) data)
    {
        if (!(bool)enableEnhancedAutopilot.GetProperty()) return;

        Autopilot autopilot = SELocator.GetShipBody().GetComponent<Autopilot>();

        if (data.abort)
        {
            autopilot.Abort();
            return;
        }
        else if (data.stopMatch)
        {
            autopilot.StopMatchVelocity();
            return;
        }

        OWRigidbody targetBody = data.targetBodyID > 0 
            ? ShipEnhancements.QSBInteraction.GetBodyFromID(data.targetBodyID) : null;
        if (targetBody == null || targetBody.GetReferenceFrame() == null) return;

        if (data.destination)
        {
            autopilot.FlyToDestination(targetBody.GetReferenceFrame());
        }
        else if (data.startMatch)
        {
            autopilot.StartMatchVelocity(targetBody.GetReferenceFrame());
        }
    }

    public void SendPidAutopilotState(uint id, bool orbit = false, bool matchPosition = false, bool abort = false)
    {
        _api.SendMessage("pid-autopilot-state", (orbit, matchPosition, abort), id, false);
    }

    private void ReceivePidAutopilotState(uint id, (bool orbit, bool matchPosition, bool abort) data)
    {
        if (!(bool)enableEnhancedAutopilot.GetProperty()) return;

        PidAutopilot autopilot = SELocator.GetShipBody().GetComponent<PidAutopilot>();

        if (data.abort)
        {
            autopilot.SetAutopilotActive(false, autopilot.GetCurrentMode(), false);
            return;
        }

        if (data.orbit)
        {
            autopilot.SetAutopilotActive(true, PidMode.Orbit, false);
        }
        else if (data.matchPosition)
        {
            autopilot.SetAutopilotActive(true, PidMode.HoldPosition, false);
        }
    }

    public void SendPersistentInput(uint id, Vector3 input)
    {
        _api.SendMessage("persistent-input", new SerializedVector3(input), id, false);
    }

    private void ReceivePersistentInput(uint id, SerializedVector3 input)
    {
        if (!(bool)enableEnhancedAutopilot.GetProperty()) return;

        SELocator.GetShipBody().GetComponent<ShipPersistentInput>().SetInputRemote(input.Vector);
    }
    #endregion

    #region ShipHorn
    public void SendHonkHorn(uint id)
    {
        _api.SendMessage("honk-horn", new NoData(), id, false);
    }

    private void ReceiveHonkHorn(uint id, NoData noData)
    {
        SELocator.GetShipTransform().GetComponentInChildren<ShipHornController>()?.PlayHorn();
    }
    #endregion

    #region ResourcePump
    public void SendPumpType(uint id, OWItem item, int typeIndex)
    {
        _api.SendMessage("pump-type", (ShipEnhancements.QSBInteraction.GetIDFromItem(item), typeIndex), id, false);
    }

    private void ReceivePumpType(uint from, (int itemID, int typeIndex) data)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        if (item == null) return;

        ResourcePump pump = item as ResourcePump;
        pump.UpdateTypeRemote(data.typeIndex);
    }

    public void SendPumpPowered(uint id, OWItem item, bool powered)
    {
        _api.SendMessage("pump-powered", (ShipEnhancements.QSBInteraction.GetIDFromItem(item), powered), id, false);
    }

    private void ReceivePumpPowered(uint from, (int itemID, bool powered) data)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        if (item == null) return;

        ResourcePump pump = item as ResourcePump;
        pump.UpdatePoweredRemote(data.powered);
    }

    public void SendPumpMode(uint id, OWItem item, bool output)
    {
        _api.SendMessage("pump-mode", (ShipEnhancements.QSBInteraction.GetIDFromItem(item), output), id, false);
    }

    private void ReceivePumpMode(uint from, (int itemID, bool output) data)
    {
        OWItem item = ShipEnhancements.QSBInteraction.GetItemFromID(data.itemID);
        if (item == null) return;

        ResourcePump pump = item as ResourcePump;
        pump.UpdateModeRemote(data.output);
    }
    #endregion

    #region WaterCooling
    public void SendWaterCoolingState(uint id, bool state)
    {
        _api.SendMessage("water-cooling", state, id, false);
    }

    private void ReceiveWaterCoolingState(uint from, bool state)
    {
        SELocator.GetShipBody().GetComponentInChildren<WaterCoolingLever>()?.OnPressInteractRemote(state);
    }
    #endregion

    #region ReactorOverload
    public void SendReactorOverload(uint id, bool overload)
    {
        _api.SendMessage("reactor-overload", overload, id, false);
    }

    private void ReceiveReactorOverload(uint from, bool overload)
    {
        GameObject.FindObjectOfType<ReactorOverloader>().SetOverloadedRemote(overload);
    }
    #endregion
}
