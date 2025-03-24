using UnityEngine;

namespace ShipEnhancements;

public class TetherAudioController : MonoBehaviour
{
    [SerializeField]
    private OWAudioSource _reelSource;
    [SerializeField]
    private OWAudioSource _lineSource;
    [SerializeField]
    private AudioClip _reelInAudio;
    [SerializeField]
    private AudioClip _reelOutAudio;
    [SerializeField]
    private AudioClip _lineAudio;

    private void Start()
    {
        _lineSource.clip = _lineAudio;
    }

    private void Update()
    {
        if (_reelSource.isPlaying)
        {
            float distRatio = Mathf.InverseLerp(20f, 0.5f, ShipEnhancements.Instance.playerTether.GetDistanceToTetherEnd());
            _reelSource.SetLocalVolume(Mathf.Lerp(0.1f, 1f, Mathf.Pow(distRatio, 2)));
        }
    }

    public void PlayReelAudio(bool reelingIn)
    {
        if (_reelSource.isPlaying)
        {
            _reelSource.Stop();
        }

        if (reelingIn)
        {
            _reelSource.clip = _reelInAudio;
        }
        else
        {
            _reelSource.clip = _reelOutAudio;
        }

        _reelSource.Play();
    }

    public void StopReelAudio()
    {
        _reelSource.Stop();
    }
}
