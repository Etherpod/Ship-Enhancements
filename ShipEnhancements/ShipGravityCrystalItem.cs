using System;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipGravityCrystalItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.GravityCrystalType;

    [SerializeField]
    private DirectionalForceVolume _forceVolume;
    [SerializeField]
    private Light _light;
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private GameObject _meshParent;
    [SerializeField]
    private GameObject _brokenMesh;

    private ShipGravityComponent _gravityComponent;
    private Light _gravityComponentLight;
    private bool _hasBeenSocketed = false;
    private float _baseFieldStrength;

    public override string GetDisplayName()
    {
        return "Gravity Crystal";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _gravityComponent = SELocator.GetShipTransform().GetComponentInChildren<ShipGravityComponent>();
        _gravityComponentLight = _gravityComponent.transform.Find("Light_NOM_GravityCrystal").GetComponent<Light>();

        if (!(bool)disableGravityCrystal.GetProperty())
        {
            _gravityComponent.OnDamaged += OnGravityDamaged;
            _gravityComponent.OnRepaired += OnGravityRepaired;
        }
    }

    private void Start()
    {
        _forceVolume.SetVolumeActivation(false);
        _light.enabled = false;
        _baseFieldStrength = _gravityComponent._gravityVolume._fieldMagnitude;
        _forceVolume.SetFieldMagnitude(_baseFieldStrength * (float)gravityMultiplier.GetProperty());
        if (!(bool)disableGravityCrystal.GetProperty() && !_gravityComponent.isDamaged)
        {
            _audioSource.AssignAudioLibraryClip(AudioType.NomaiGravCrystalAmbient_LP);
        }
        else
        {
            _audioSource.AssignAudioLibraryClip(AudioType.NomaiGravCrystalFlickerAmbient_LP);
        }
        _brokenMesh.SetActive((bool)disableGravityCrystal.GetProperty());
        _meshParent.SetActive(false);
    }
    
    private void OnGravityDamaged(ShipComponent component)
    {
        _brokenMesh.SetActive(true);
        _audioSource.AssignAudioLibraryClip(AudioType.NomaiGravCrystalFlickerAmbient_LP);
    }

    private void OnGravityRepaired(ShipComponent component)
    {
        _brokenMesh.SetActive(false);
        _audioSource.AssignAudioLibraryClip(AudioType.NomaiGravCrystalAmbient_LP);
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);

        transform.localScale = Vector3.one;

        _light.enabled = !(bool)disableGravityCrystal.GetProperty();

        if (!(bool)disableGravityCrystal.GetProperty() && !_gravityComponent.isDamaged)
        {
            _forceVolume.SetAttachedBody(parent.GetAttachedOWRigidbody());
            _forceVolume.SetVolumeActivation(true);
        }
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);

        transform.localPosition = new Vector3(0f, -0.3f, 0f);
        transform.localScale = Vector3.one * 0.5f;

        _meshParent.SetActive(true);
        _gravityComponent.OnComponentDamaged();
        _light.enabled = false;

        _gravityComponent._gravityAudio.FadeOut(0.5f);
        _gravityComponentLight.enabled = false;

        if (!(bool)disableGravityCrystal.GetProperty() && !_gravityComponent.isDamaged)
        {
            _forceVolume.SetVolumeActivation(false);
        }      
        else
        {
            _gravityComponent._damageEffect._decalRenderers[0].SetActivation(false);
            _gravityComponent._damageEffect._particleSystem.Stop();
        }
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);

        if (_hasBeenSocketed)
        {
            transform.localScale = Vector3.one;

            _meshParent.SetActive(false);
            _light.enabled = false;

            if (!(bool)disableGravityCrystal.GetProperty() && !_gravityComponent.isDamaged)
            {
                _gravityComponent.OnComponentRepaired();
                _gravityComponent._gravityAudio.FadeIn(0.5f);
            }
            else
            {
                if (!(bool)disableGravityCrystal.GetProperty())
                {
                    _gravityComponentLight.enabled = true;
                    _gravityComponent._damageEffect._particleSystem.Play();
                }
                _gravityComponent._damageEffect._decalRenderers[0].SetActivation(true);
            }
        }
        else
        {
            _hasBeenSocketed = true;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!(bool)disableGravityCrystal.GetProperty())
        {
            _gravityComponent.OnDamaged -= OnGravityDamaged;
            _gravityComponent.OnRepaired -= OnGravityRepaired;
        }
    }
}
