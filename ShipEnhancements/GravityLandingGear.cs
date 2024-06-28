using UnityEngine;

namespace ShipEnhancements;

public class GravityLandingGear : MonoBehaviour
{
    private ShipDamageController _damageController;
    private ShipLandingGear _landingGear;
    private OWAudioSource _audioSource;
    private GameObject _gravityEffects;
    private float _gravityMagnitude = 10f;
    private bool _gravityEnabled = false;
    private bool _shipDestroyed = false;
    private bool _damaged;
    private bool _landed = false;

    private void Start()
    {
        _damageController = Locator.GetShipBody().GetComponent<ShipDamageController>();
        _landingGear = GetComponentInParent<ShipLandingGear>();
        GameObject audioObject = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/Audio_GravityLandingGear.prefab");
        _audioSource = Instantiate(audioObject, transform).GetComponent<OWAudioSource>();
        GameObject effectsObject = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/Effects_GravityLandingGear_WarpParticles.prefab");
        _gravityEffects = Instantiate(effectsObject, transform);

        ShipEnhancements.Instance.OnGravityLandingGearSwitch += SetGravityEnabled;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        _landingGear.OnDamaged += ctx => OnLandingGearDamaged();
        _landingGear.OnRepaired += ctx => OnLandingGearRepaired();

        _damaged = _landingGear.isDamaged;
    }

    public void SetGravityEnabled(bool enabled)
    {
        _gravityEnabled = enabled;
        if (!enabled)
        {
            if (_gravityEffects.GetComponent<ParticleSystem>().isPlaying)
            {
                _gravityEffects.GetComponent<ParticleSystem>().Stop();
            }
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
                _audioSource.time = 0f;
            }
        }
        else if (enabled)
        {
            if (_landed && !_gravityEffects.GetComponent<ParticleSystem>().isPlaying)
            {
                _gravityEffects.GetComponent<ParticleSystem>().Play();
            }
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
                _audioSource.pitch = 1f;
            }
        }
    }

    private void OnTriggerStay(Collider hitCollider)
    {
        if (_shipDestroyed || _landingGear.isDamaged || !_gravityEnabled)
        {
            return;
        }

        if (hitCollider.attachedRigidbody != null)
        {
            if (hitCollider.attachedRigidbody.isKinematic)
            {
                Locator.GetShipBody().AddAcceleration(-transform.up * _gravityMagnitude);
            }
            else
            {
                Locator.GetShipBody().AddAcceleration(-transform.up * (_gravityMagnitude / 10) * (1 / Locator.GetShipBody().GetMass()));
                hitCollider.attachedRigidbody.GetAttachedOWRigidbody().AddAcceleration(transform.up * (_gravityMagnitude / 10) * (1 / hitCollider.attachedRigidbody.mass));
            }
        }
    }

    private void OnTriggerEnter(Collider hitCollider)
    {
        if (hitCollider.attachedRigidbody != null)
        {
            _landed = true;
            if (!_shipDestroyed && !_damaged && _gravityEnabled)
            {
                _gravityEffects.GetComponent<ParticleSystem>().Play();
            }
        }
    }

    private void OnTriggerExit(Collider hitCollider)
    {
        if (hitCollider.attachedRigidbody != null)
        {
            _landed = false;
           _gravityEffects.GetComponent<ParticleSystem>().Stop();
        }
    }

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
        SetGravityEnabled(false);
    }

    private void OnLandingGearDamaged()
    {
        _damaged = true;
        SetGravityEnabled(false);
    }

    private void OnLandingGearRepaired()
    {
        _damaged = false;
        SetGravityEnabled(true);
    }

    private void OnDestroy()
    {
        ShipEnhancements.Instance.OnGravityLandingGearSwitch -= SetGravityEnabled;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        _landingGear.OnDamaged -= ctx => OnLandingGearDamaged();
        _landingGear.OnRepaired -= ctx => OnLandingGearRepaired();
    }
}
