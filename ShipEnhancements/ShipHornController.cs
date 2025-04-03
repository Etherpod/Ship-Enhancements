using UnityEngine;

namespace ShipEnhancements;

public class ShipHornController : MonoBehaviour
{
    private OWAudioSource _hornSource;

    private void Awake()
    {
        _hornSource = gameObject.GetRequiredComponent<OWAudioSource>();
    }

    private void Start()
    {
        _hornSource.clip = ShipEnhancements.Instance.ShipHorn;
    }

    public void PlayHorn()
    {
        if (_hornSource.isPlaying) return;

        _hornSource.time = 0;
        _hornSource.Play();
    }
}
