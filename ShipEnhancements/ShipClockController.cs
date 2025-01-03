using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEnhancements;

public class ShipClockController : MonoBehaviour
{
    [SerializeField]
    private Transform _minuteHand;
    [SerializeField]
    private Transform _hourHand;
    [SerializeField]
    private RotateTransform _gear;
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private AudioClip[] _tickSounds;

    private int _lastSeconds;
    private int _lastTickIndex = -1;
    private bool _shipDestroyed = false;

    private void Awake()
    {
        GlobalMessenger.AddListener("EnterShip", OnEnterShip);
        GlobalMessenger.AddListener("ExitShip", OnExitShip);
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);

        _gear.enabled = false;
        _lastSeconds = 0;
        enabled = false;
    }

    private void Update()
    {
        int seconds = (int)TimeLoop.GetSecondsElapsed() % 60;
        int minutes = (int)TimeLoop.GetMinutesElapsed() % 60;

        if (seconds != _lastSeconds)
        {
            _minuteHand.transform.localRotation = Quaternion.Euler(0f, 180f + 6f * seconds, 0f);
            _hourHand.transform.localRotation = Quaternion.Euler(0f, 180f + 6f * minutes, 0f);

            List<AudioClip> clips = [.. _tickSounds];
            AudioClip nextClip = _tickSounds.Where(clip => clips.IndexOf(clip) != _lastTickIndex).ToArray()[Random.Range(0, _tickSounds.Length - 1)];
            _audioSource.PlayOneShot(nextClip, 0.1f);
            _lastTickIndex = clips.IndexOf(nextClip);
            _lastSeconds = seconds;
        }
    }

    private void OnEnterShip()
    {
        if (!_shipDestroyed)
        {
            _gear.enabled = true;
            enabled = true;
        }
    }

    private void OnExitShip()
    {
        if (!_shipDestroyed)
        {
            _gear.enabled = false;
            enabled = false;
        }
    }

    private void OnShipSystemFailure()
    {
        _gear.enabled = false;
        enabled = false;
        _shipDestroyed = true;
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
        GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
    }
}
