using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class QSBCompatibility
{
    private readonly IQSBAPI _api;
    private List<CockpitSwitch> _activeSwitches = [];
    private ShipEngineSwitch _engineSwitch;
    private List<TetherHookItem> _activeTetherHooks = [];
    private bool _neverInitialized = true;

    [Serializable]
    private struct NoData { }

    public QSBCompatibility(IQSBAPI api)
    {
        _api = api;
        _api.OnPlayerJoin().AddListener(OnPlayerJoin);
        _api.RegisterHandler<(string, object)>("settings-data", ReceiveSettingsData);
        _api.RegisterHandler<(string, bool)>("switch-state", ReceiveSwitchState);
        _api.RegisterHandler<NoData>("ship-initialized", ReceiveInitializedShip);
        _api.RegisterHandler<NoData>("world-objects-ready", ReceiveWorldObjectsInitialized);
        _api.RegisterHandler<bool>("engine-switch-state", ReceiveEngineSwitchState);
        _api.RegisterHandler<bool>("initialize-engine-switch", InitializeEngineSwitch);
        _api.RegisterHandler<(float, bool)>("ship-oxygen-drain", ReceiveShipOxygenDrain);
        _api.RegisterHandler<(float, bool)>("ship-fuel-drain", ReceiveShipFuelDrain);
        _api.RegisterHandler<float>("set-ship-oxygen", ReceiveShipOxygenValue);
        _api.RegisterHandler<float>("set-ship-fuel", ReceiveShipFuelValue);
        _api.RegisterHandler<bool>("panel-state", ReceivePanelExtended);
        _api.RegisterHandler<(int, bool)>("modulator-button-state", ReceiveModulatorButtonState);
        _api.RegisterHandler<(bool, bool, bool)>("overdrive-button-state", ReceiveOverdriveButtonState);
        _api.RegisterHandler<NoData>("overdrive-stop-coroutines", ReceiveStopOverdriveCoroutines);
        _api.RegisterHandler<NoData>("campfire-reactor-damage", ReceiveCampfireReactorDamaged);
        _api.RegisterHandler<NoData>("campfire-extinguished", ReceiveCampfireExtinguished);
        _api.RegisterHandler<(bool, bool, bool)>("campfire-initial-state", ReceiveCampfireInitialState);
        _api.RegisterHandler<float>("ship-temp-meter", ReceiveShipHullTemp);
        _api.RegisterHandler<int>("attach-tether", ReceiveAttachTether);
        _api.RegisterHandler<int>("disconnect-tether", ReceiveDisconnectTether);
        _api.RegisterHandler<(int, int)>("transfer-tether", ReceiveTransferTether);
        _api.RegisterHandler<NoData>("ship-fuel-max", ReceiveShipFuelMax);
        _api.RegisterHandler<(int, float, float)>("rust-state", ReceiveInitialRustState);
        _api.RegisterHandler<(float, float, float)>("initial-dirt-state", ReceiveInitialDirtState);
        _api.RegisterHandler<float>("dirt-state", ReceiveDirtState);
    }

    private void OnPlayerJoin(uint playerID)
    {
        if (!_api.GetIsHost() || _api.GetLocalPlayerID() == playerID)
        {
            return;
        }

        _neverInitialized = true;

        SendSettingsData(playerID);
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

    private void ReceiveSettingsData(uint id, (string, object) data)
    {
        var allSettings = Enum.GetValues(typeof(ShipEnhancements.Settings)) as ShipEnhancements.Settings[];
        foreach (var setting in allSettings)
        {
            if (setting.GetName() == data.Item1)
            {
                setting.SetProperty(data.Item2);
                return;
            }
        }
        ShipEnhancements.WriteDebugMessage($"Setting {data.Item1} not found", error: true);
    }
    #endregion

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

        if (_activeSwitches.Count > 0)
        {
            foreach (CockpitSwitch cockpitSwitch in _activeSwitches)
            {
                SendSwitchState(id, (cockpitSwitch.GetType().Name, cockpitSwitch.IsOn()));
            }
        }
        if (_engineSwitch != null)
        {
            _api.SendMessage("initialize-engine-switch", ShipEnhancements.Instance.engineOn, id, false);
        }
        if ((bool)ShipEnhancements.Settings.addPortableCampfire.GetProperty())
        {
            bool dropped = false;
            bool unpacked = false;
            bool lit = false;
            PortableCampfireItem item = SELocator.GetPortableCampfire().GetComponentInParent<PortableCampfireItem>();
            if (item.IsDropped())
            {
                dropped = true;
                if (item.IsUnpacked())
                {
                    unpacked = true;
                    if (!SELocator.GetPortableCampfire().IsExtinguished())
                    {
                        lit = true;
                    }
                }
            }
            SendCampfireInitialState(id, dropped, unpacked, lit);
        }
        if ((float)ShipEnhancements.Settings.rustLevel.GetProperty() > 0)
        {
            SELocator.GetCockpitFilthController()?.BroadcastInitialRustState();
        }
        if ((float)ShipEnhancements.Settings.dirtAccumulationTime.GetProperty() > 0f)
        {
            SELocator.GetCockpitFilthController()?.BroadcastInitialDirtState();
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
                ShipEnhancements.WriteDebugMessage("same: " + (hook.GetTether() == hook.GetActiveTether()));
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

    #region Switches
    public void AddActiveSwitch(CockpitSwitch switchToAdd)
    {
        _activeSwitches.Add(switchToAdd);
    }

    public void RemoveActiveSwitch(CockpitSwitch switchToRemove)
    {
        if (_activeSwitches.Contains(switchToRemove))
        {
            _activeSwitches.Remove(switchToRemove);
        }
    }

    public void SendSwitchState(uint id, (string, bool) data)
    {
        _api.SendMessage("switch-state", data, id, false);
    }

    private void ReceiveSwitchState(uint id, (string, bool) data)
    {
        foreach (CockpitSwitch cockpitSwitch in _activeSwitches)
        {
            if (cockpitSwitch.GetType().Name == data.Item1)
            {
                cockpitSwitch.ChangeSwitchState(data.Item2);
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

    public void SendEngineSwitchState(uint id, bool wasPressed)
    {
        _api.SendMessage("engine-switch-state", wasPressed, id, false);
    }

    private void ReceiveEngineSwitchState(uint id, bool wasPressed)
    {
        _engineSwitch?.UpdateWasPressed(wasPressed);
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
    public void SendCampfireReactorDamaged(uint id)
    {
        _api.SendMessage("campfire-reactor-delay", new NoData(), id, false);
    }

    private void ReceiveCampfireReactorDamaged(uint id, NoData noData)
    {
        SELocator.GetPortableCampfire()?.OnRemoteReactorDamaged();
    }

    public void SendCampfireExtinguishState(uint id)
    {
        _api.SendMessage("campfire-extinguished", new NoData(), id, false);
    }

    private void ReceiveCampfireExtinguished(uint id, NoData noData)
    {
        SELocator.GetPortableCampfire().OnExtinguishInteract();
    }

    public void SendCampfireInitialState(uint id, bool dropped, bool unpacked, bool lit)
    {
        _api.SendMessage("campfire-initial-state", (dropped, unpacked, lit), id, false);
    }

    private void ReceiveCampfireInitialState(uint id, (bool, bool, bool) data)
    {
        PortableCampfireItem item = SELocator.GetPortableCampfire().GetComponentInParent<PortableCampfireItem>();
        if (!item.IsDropped()) return;

        if (data.Item1)
        {
            item.TogglePackUp(false);

            if (data.Item2)
            {
                SELocator.GetPortableCampfire().SetInitialState(Campfire.State.LIT);
                SELocator.GetPortableCampfire().SetState(Campfire.State.LIT);
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
        SELocator.GetShipTemperatureDetector()?.SetShipTempMeter(temperature);
    }
    #endregion

    #region Tether
    public void SendAttachTether(uint id, TetherHookItem hook)
    {
        _api.SendMessage("attach-tether", ShipEnhancements.QSBInteraction.GetIDFromTetherHook(hook), id, false);
    }

    private void ReceiveAttachTether(uint id, int hookID)
    {
        if (!ShipEnhancements.QSBInteraction.WorldObjectsLoaded()) return;
        ShipEnhancements.QSBInteraction.GetTetherHookFromID(hookID).OnConnectTetherRemote(id);
    }

    public void SendDisconnectTether(uint id, TetherHookItem hook)
    {
        _api.SendMessage("disconnect-tether", ShipEnhancements.QSBInteraction.GetIDFromTetherHook(hook), id, false);
    }

    private void ReceiveDisconnectTether(uint id, int hookID)
    {
        if (!ShipEnhancements.QSBInteraction.WorldObjectsLoaded()) return;
        ShipEnhancements.QSBInteraction.GetTetherHookFromID(hookID).OnDisconnectTetherRemote();
    }

    public void SendTransferTether(uint id, TetherHookItem newHook, TetherHookItem lastHook)
    {
        int newID = ShipEnhancements.QSBInteraction.GetIDFromTetherHook(newHook);
        int lastID = ShipEnhancements.QSBInteraction.GetIDFromTetherHook(lastHook);
        _api.SendMessage("transfer-tether", (newID, lastID), id, false);
    }

    private void ReceiveTransferTether(uint id, (int newID, int lastID) data)
    {
        if (!ShipEnhancements.QSBInteraction.WorldObjectsLoaded()) return;
        Tether newTether = ShipEnhancements.QSBInteraction.GetTetherHookFromID(data.lastID).GetTether();
        ShipEnhancements.QSBInteraction.GetTetherHookFromID(data.newID).OnTransferRemote(newTether);
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

    public void SendInitialRustState(uint id, int textureIndex, Vector2 textureOffset)
    {
        _api.SendMessage("rust-state", (textureIndex, textureOffset.x, textureOffset.y), id, false);
    }

    private void ReceiveInitialRustState(uint id, (int textureIndex, float offsetX, float offsetY) data)
    {
        SELocator.GetCockpitFilthController()?.SetInitialRustState(data.textureIndex, new Vector2(data.offsetX, data.offsetY));
    }

    public void SendInitialDirtState(uint id, Vector2 textureOffset, float currentProgression)
    {
        _api.SendMessage("initial-dirt-state", (textureOffset.x, textureOffset.y, currentProgression), id, false);
    }

    private void ReceiveInitialDirtState(uint id, (float offsetX, float offsetY, float currentProgression) data)
    {
        SELocator.GetCockpitFilthController()?.SetInitialDirtState(new Vector2(data.offsetX, data.offsetY), data.currentProgression);
    }

    public void SendDirtState(uint id, float progression)
    {
        _api.SendMessage("dirt-state", progression, id, false);
    }

    private void ReceiveDirtState(uint id, float progression)
    {
        SELocator.GetCockpitFilthController()?.UpdateDirtState(progression);
    }

    #endregion
}
