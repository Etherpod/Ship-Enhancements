using UnityEngine;

namespace ShipEnhancements;

public class CockpitCurtainController : MonoBehaviour
{
    [SerializeField]
    private GameObject _closedCurtainObj;
    [SerializeField]
    private GameObject _openCurtainObj;
    [SerializeField]
    private InteractReceiver _interactReceiver;
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private AudioClip _openClip;
    [SerializeField]
    private AudioClip _closeClip;

    private bool _open = true;

    private void Awake()
    {
        _interactReceiver.OnPressInteract += OnPressInteract;
    }

    private void Start()
    {
        UpdateCurtain();
    }

    private void OnPressInteract()
    {
        _open = !_open;
        UpdateCurtain();

        if (_audioSource.isPlaying) _audioSource.Stop();
        _audioSource.clip = _open ? _openClip : _closeClip;
        _audioSource.Play();

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendCurtainState(id, _open);
            }
        }
    }

    private void UpdateCurtain()
    {
        _closedCurtainObj.SetActive(!_open);
        _openCurtainObj.SetActive(_open);
        _interactReceiver.ChangePrompt(_open ? "Close Curtain" : "Open Curtain");
    }

    public void UpdateCurtainRemote(bool open)
    {
        _closedCurtainObj.SetActive(!open);
        _openCurtainObj.SetActive(open);
        _interactReceiver.ChangePrompt(open ? "Close Curtain" : "Open Curtain");

        if (_audioSource.isPlaying) _audioSource.Stop();
        _audioSource.clip = _open ? _openClip : _closeClip;
        _audioSource.Play();
    }
}
