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

    private float _lastTetherPos;

    private void Awake()
    {
        GlobalMessenger.AddListener("AttachPlayerTether", OnAttachPlayerTether);
        GlobalMessenger.AddListener("DetachPlayerTether", OnDetachPlayerTether);

        enabled = false;
    }

    private void Update()
    {
        Tether tether = ShipEnhancements.Instance.playerTether;

        if (tether == null) return;

        float dist = tether.GetDistanceToTetherEnd();

        if (_reelSource.isPlaying)
        {
            float distRatio = Mathf.InverseLerp(20f, 0.5f, dist);
            _reelSource.SetLocalVolume(Mathf.Lerp(0.1f, 1f, Mathf.Pow(distRatio, 2)));
        }

        float diff = _lastTetherPos - dist;
        if (Mathf.Abs(diff) > 1.5f)
        {
            if (diff > 0f)
            {
                float distRatio = Mathf.InverseLerp(15f, 0f, dist);
                _lineSource.pitch = Random.Range(0.95f, 1.05f);
                _lineSource.PlayOneShot(_lineAudio, Mathf.Lerp(0.25f, 1f, Mathf.Pow(distRatio, 2)));
            }
            _lastTetherPos = dist;
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

    private void OnAttachPlayerTether()
    {
        enabled = true;
        _lastTetherPos = ShipEnhancements.Instance.playerTether.GetDistanceToTetherEnd();
    }

    private void OnDetachPlayerTether()
    {
        StopReelAudio();
        enabled = false;
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("AttachPlayerTether", OnAttachPlayerTether);
        GlobalMessenger.RemoveListener("DetachPlayerTether", OnDetachPlayerTether);
    }
}
