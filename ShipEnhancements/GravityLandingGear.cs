using UnityEngine;

namespace ShipEnhancements;

public class GravityLandingGear : MonoBehaviour
{
    private ShipLandingGear _landingGear;
    private OWAudioSource _audioSource;
    private ParticleSystem _gravityEffects;
    private float _gravityMagnitude = 10f;
    private bool _gravityEnabled = false;
    private bool _shipDestroyed = false;
    private bool _damaged;
    private bool _landed = false;

    private void Start()
    {
        _landingGear = GetComponentInParent<ShipLandingGear>();
        GameObject audioObject = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/Audio_GravityLandingGear.prefab");
        _audioSource = Instantiate(audioObject, transform).GetComponent<OWAudioSource>();
        GameObject effectsObject = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/Effects_GravityLandingGear_WarpParticles.prefab");
        _gravityEffects = Instantiate(effectsObject, transform).GetComponent<ParticleSystem>();

        ShipEnhancements.Instance.OnGravityLandingGearSwitch += SetGravityEnabled;
        GlobalMessenger.AddListener("ShipSystemFailure", OnShipSystemFailure);
        _landingGear.OnDamaged += ctx => OnLandingGearDamaged();
        _landingGear.OnRepaired += ctx => OnLandingGearRepaired();
        ShipEnhancements.Instance.OnEngineStateChanged += OnEngineStateChanged;

        _damaged = _landingGear.isDamaged;
    }

    public void SetGravityEnabled(bool enabled)
    {
        _gravityEnabled = enabled;
        if (!enabled)
        {
            if (_gravityEffects.isPlaying)
            {
                _gravityEffects.Stop();
            }
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
                _audioSource.time = 0f;
            }
        }
        else if (enabled && !_damaged && !_shipDestroyed)
        {
            if (_landed && !_gravityEffects.isPlaying)
            {
                _gravityEffects.Play();
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
        if (_shipDestroyed || _landingGear.isDamaged || !_gravityEnabled || !ShipEnhancements.Instance.engineOn)
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
                _gravityEffects.Play();
            }
        }
    }

    private void OnTriggerExit(Collider hitCollider)
    {
        if (hitCollider.attachedRigidbody != null)
        {
            _landed = false;
           _gravityEffects.Stop();
        }
    }

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
        if (_gravityEffects.isPlaying)
        {
            _gravityEffects.Stop();
        }
        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
            _audioSource.time = 0f;
        }
    }

    private void OnEngineStateChanged(bool enabled)
    {
        if (enabled && _gravityEnabled && !_damaged && !_shipDestroyed)
        {
            if (_landed && !_gravityEffects.isPlaying)
            {
                _gravityEffects.Play();
            }
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
                _audioSource.pitch = 1f;
            }
        }
        else
        {
            if (_gravityEffects.isPlaying)
            {
                _gravityEffects.Stop();
            }
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
                _audioSource.time = 0f;
            }
        }
    }

    private void OnLandingGearDamaged()
    {
        _damaged = true;
        if (_gravityEffects.isPlaying)
        {
            _gravityEffects.Stop();
        }
        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
            _audioSource.time = 0f;
        }
    }

    private void OnLandingGearRepaired()
    {
        _damaged = false;

        if (_shipDestroyed || !_gravityEnabled) return;

        if (_landed)
        {
            _gravityEffects.Play();
        }
        _audioSource.Play();
        _audioSource.pitch = 1f;
    }

    private void OnDestroy()
    {
        ShipEnhancements.Instance.OnGravityLandingGearSwitch -= SetGravityEnabled;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        _landingGear.OnDamaged -= ctx => OnLandingGearDamaged();
        _landingGear.OnRepaired -= ctx => OnLandingGearRepaired();
    }
}
