using UnityEngine;

namespace ShipEnhancements;

public class SmokeDetectorChirp : MonoBehaviour
{
    private float _timeUntilChirp;
    private readonly float _minInterval = 10f;
    private readonly float _maxInterval = 45f;
    private AudioClip _chripClip;
    private OWAudioSource _chirpSource;

    private void Awake()
    {
        _chripClip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/chirp_of_death.ogg");
        _chirpSource = SELocator.GetShipTransform()
            .Find("Audio_Ship/ShipInteriorAudio/ComputerAudioSource")
            .GetComponent<OWAudioSource>();
        _timeUntilChirp = Random.Range(_minInterval, _maxInterval) * 60f;
    }

    private void Update()
    {
        if (_timeUntilChirp <= 0f)
        {
            _chirpSource.PlayOneShot(_chripClip, 0.9f);
            _timeUntilChirp = Random.Range(_minInterval, _maxInterval) * 60f;
        }
        else if (PlayerState.IsInsideShip())
        {
            _timeUntilChirp -= Time.deltaTime;
        }
    }
}
