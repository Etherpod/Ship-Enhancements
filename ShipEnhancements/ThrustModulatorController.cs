using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ThrustModulatorController : ElectricalComponent
{
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private AudioClip _overdriveButtonAudio;
    [SerializeField]
    [ColorUsage(true, true)]
    private Color _overdriveButtonColor = Color.blue;

    private ThrustModulatorButton[] _modulatorButtons;
    private CockpitButtonPanel _buttonPanel;
    private int _lastLevel;
    private int _focusedButtons;
    private bool _focused = false;
    private ElectricalSystem _electricalSystem;
    private bool _electricalDisrupted = false;
    private bool _lastPoweredState = false;
    private Coroutine _overdriveSequence;

    public override void Awake()
    {
        _powered = true;

        base.Awake();

        _modulatorButtons = GetComponentsInChildren<ThrustModulatorButton>();
        ShipEnhancements.Instance.SetThrustModulatorLevel(5);

        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _electricalSystem = SELocator.GetShipTransform()
            .Find("Module_Cockpit/Systems_Cockpit/FlightControlsElectricalSystem")
            .GetComponent<ElectricalSystem>();
        List<ElectricalComponent> componentList = [.. _electricalSystem._connectedComponents];
        componentList.Add(this);
        _electricalSystem._connectedComponents = [.. componentList];
    }

    private void Start()
    {
        if (!(bool)enableThrustModulator.GetProperty()) return;
        UpdateModulatorDisplay(5);
    }

    private IEnumerator OverdriveSequence()
    {
        DisableModulatorDisplay();
        yield return new WaitForSeconds(0.6f);
        _audioSource.pitch = 1f;
        for (int i = 0; i < _modulatorButtons.Length; i++)
        {
            _modulatorButtons[i].SetButtonLight(true, true);
            _modulatorButtons[i].SetButtonColor(_overdriveButtonColor);
            _audioSource.pitch += Random.Range(0.3f, 0.5f);
            _audioSource.PlayOneShot(_overdriveButtonAudio, 0.8f);
            yield return new WaitForSeconds(0.4f);
        }
        _overdriveSequence = null;
    }

    private IEnumerator ResetModulator()
    {
        foreach (ThrustModulatorButton button in _modulatorButtons)
        {
            button.TurnOffAndReset();
            button.SetInteractable(false);
        }

        yield return new WaitForSeconds(0.5f);

        foreach (ThrustModulatorButton button in _modulatorButtons)
        {
            if (_electricalSystem.IsPowered() &&
                !(SELocator.GetShipDamageController().IsElectricalFailed() || SELocator.GetShipDamageController().IsSystemFailed()))
            {
                button.SetButtonLight(button.GetModulatorLevel() <= _lastLevel);
                button.SetInteractable(button.GetModulatorLevel() != _lastLevel);
            }
        }
    }

    public void UpdateModulatorDisplay(int setLevel, bool disable = true)
    {
        StopAllCoroutines();
        if (setLevel > 0)
        {
            _lastLevel = setLevel;
        }
        foreach (ThrustModulatorButton button in _modulatorButtons)
        {
            button.SetButtonLight(button.GetModulatorLevel() <= setLevel);
            button.SetInteractable(!disable || button.GetModulatorLevel() != setLevel);
        }
    }

    public void DisableModulatorDisplay()
    {
        foreach (ThrustModulatorButton button in _modulatorButtons)
        {
            button.SetButtonLight(false);
            button.SetInteractable(false);
        }
    }

    public void BeginOverdriveSequence()
    {
        if (_overdriveSequence == null)
        {
            StopAllCoroutines();
            _overdriveSequence = StartCoroutine(OverdriveSequence());
        }
    }

    public void EndOverdriveSequence()
    {
        if (_overdriveSequence != null)
        {
            StopCoroutine(_overdriveSequence);
            _overdriveSequence = null;
        }
        StartCoroutine(ResetModulator());
    }

    private void OnShipSystemFailure()
    {
        enabled = false;
    }

    public override void SetPowered(bool powered)
    {
        if (_electricalSystem != null && _electricalDisrupted != _electricalSystem.IsDisrupted())
        {
            _electricalDisrupted = _electricalSystem.IsDisrupted();
            _lastPoweredState = _powered;
        }

        if (!(bool)enableThrustModulator.GetProperty()) return;

        base.SetPowered(powered);

        if (!_electricalDisrupted)
        {
            StopAllCoroutines();
            if (powered)
            {
                UpdateModulatorDisplay(_lastLevel);
            }
            else
            {
                DisableModulatorDisplay();
            }
        }
        else
        {
            foreach (ThrustModulatorButton button in _modulatorButtons)
            {
                button.SetButtonLight(powered && button.GetModulatorLevel() <= _lastLevel, true);
            }
        }
    }

    public void PlayButtonSound(AudioClip clip, float volume, int level)
    {
        _audioSource.pitch = level < _lastLevel ? Random.Range(0.9f, 1f) : Random.Range(1f, 1.1f);
        _audioSource.PlayOneShot(clip, volume);
    }

    public ThrustModulatorButton GetModulatorButton(int level)
    {
        foreach (ThrustModulatorButton button in _modulatorButtons)
        {
            if (button.GetModulatorLevel() == level)
            {
                return button;
            }
        }

        ShipEnhancements.WriteDebugMessage("Could not find modulator button of level " + level);
        return null;
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
