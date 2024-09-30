using UnityEngine;
using System.Collections;

namespace ShipEnhancements;

public class PortableTractorBeamItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.portableTractorBeamType;

    [SerializeField] 
    private GameObject _tractorBeamObj;
    [SerializeField]
    private GameObject _displayedItemObj;
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private GameObject _beamVolumeParent;

    private EffectVolume[] _volumes;
    private bool _socketed;

    public override string GetDisplayName()
    {
        return "Portable Tractor Beam";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _volumes = GetComponentsInChildren<EffectVolume>(true);
    }

    private void Start()
    {
        TogglePackUp(true);
        _socketed = true;
        _beamVolumeParent.SetActive(false);
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        TogglePackUp(false);
        UpdateAttachedBody(parent.GetAttachedOWRigidbody());
        StartCoroutine(ActivationDelay());
        _audioSource.Stop();
        _audioSource.AssignAudioLibraryClip(AudioType.NomaiTractorBeamActivate);
        _audioSource.Play();
        _socketed = false;
        transform.localScale = Vector3.one;
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        StopAllCoroutines();
        _beamVolumeParent.SetActive(false);
        TogglePackUp(true);
        if (!_socketed)
        {
            _audioSource.Stop();
            _audioSource.AssignAudioLibraryClip(AudioType.NomaiTractorBeamDeactivate);
            _audioSource.Play();
        }
        _socketed = false;
        transform.localScale = Vector3.one * 0.5f;
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        ShipEnhancements.WriteDebugMessage("Socketed");
        base.SocketItem(socketTransform, sector);
        _socketed = true;
        transform.localScale = Vector3.one * 0.5f;
    }

    private void TogglePackUp(bool packUp)
    {
        _tractorBeamObj.SetActive(!packUp);
        _displayedItemObj.SetActive(packUp);
    }

    private IEnumerator ActivationDelay()
    {
        yield return new WaitForSeconds(0.2f);
        _beamVolumeParent.SetActive(true);
    }

    private void UpdateAttachedBody(OWRigidbody body)
    {
        foreach (EffectVolume vol in _volumes)
        {
            vol.SetAttachedBody(body);
        }
    }
}
