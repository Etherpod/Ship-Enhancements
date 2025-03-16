using UnityEngine;

namespace ShipEnhancements;

public class GravityLandingPad : MonoBehaviour
{
    [SerializeField]
    private OWAudioSource _audioSource;
    [SerializeField]
    private ParticleSystem _particleEffect;
    [SerializeField]
    private GravityRepelVolume _weakRepelVol;
    [SerializeField]
    private GravityRepelVolume _midRepelVol;
    [SerializeField]
    private GravityRepelVolume _strongRepelVol;
    [SerializeField]
    private GravityRepelVolume _baseRepelVol;

    private ShipLandingGear _landingGear;
    private ShipThrusterModel _thrusterModel;
    private float _gravityMagnitude = 10f;
    private bool _inverted = false;
    private bool _gravityEnabled = false;
    private bool _shipDestroyed = false;
    private bool _damaged;
    private bool _landed = false;

    private void Start()
    {
        _landingGear = GetComponentInParent<ShipLandingGear>();
        _thrusterModel = SELocator.GetShipBody().GetComponent<ShipThrusterModel>();

        //gameObject.layer = LayerMask.NameToLayer("PhysicalDetector");

        ShipEnhancements.Instance.OnGravityLandingGearSwitch += SetGravityEnabled;
        ShipEnhancements.Instance.OnGravityLandingGearInverted += SetGravityInverted;
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
            if (_particleEffect.isPlaying)
            {
                _particleEffect.Stop();
            }
            if (_audioSource.isPlaying)
            {
                _audioSource.FadeOut(0.5f);
                _audioSource.time = 0f;
            }
        }
        else if (enabled && !_damaged && !_shipDestroyed)
        {
            if (_landed)
            {
                _particleEffect.Play();
            }
            if (!_audioSource.isPlaying)
            {
                _audioSource.FadeIn(1f);
                _audioSource.pitch = 1f;
            }
        }
    }

    public void SetGravityInverted(bool inverted)
    {
        _inverted = inverted;
    }

    private void FixedUpdate()
    {
        if (_shipDestroyed || _landingGear.isDamaged || !_gravityEnabled || !ShipEnhancements.Instance.engineOn
            || (!_inverted && _thrusterModel.GetLocalAcceleration().y > 0f))
        {
            return;
        }

        if (_landed != _weakRepelVol.IsRepelling(!_inverted))
        {
            _landed = _weakRepelVol.IsRepelling(!_inverted);
            if (_landed)
            {
                if (!_shipDestroyed && !_damaged && _gravityEnabled)
                {
                    _particleEffect.Play();
                }
            }
            else
            {
                _particleEffect.Stop();
            }
        }

        if (_landed)
        {
            float mult = 0.25f;
            if (_baseRepelVol.IsRepelling(!_inverted))
            {
                mult = 1f;
            }
            else if (_strongRepelVol.IsRepelling(!_inverted))
            {
                mult = 0.75f;
            }
            else if (_midRepelVol.IsRepelling(!_inverted))
            {
                mult = 0.5f;
            }

            if (_inverted)
            {
                SELocator.GetShipBody().AddAcceleration(transform.up * _gravityMagnitude * mult);
            }
            else
            {
                SELocator.GetShipBody().AddAcceleration(-transform.up * _gravityMagnitude * mult * 2f);
            }
        }
    }

    private void OnShipSystemFailure()
    {
        _shipDestroyed = true;
        if (_particleEffect.isPlaying)
        {
            _particleEffect.Stop();
        }
        if (_audioSource.isPlaying)
        {
            _audioSource.FadeOut(0.5f);
            _audioSource.time = 0f;
        }
    }

    private void OnEngineStateChanged(bool enabled)
    {
        if (enabled && _gravityEnabled && !_damaged && !_shipDestroyed)
        {
            if (_landed)
            {
                _particleEffect.Play();
            }
            if (!_audioSource.isPlaying)
            {
                _audioSource.FadeIn(1f);
                _audioSource.pitch = 1f;
            }
        }
        else
        {
            if (_particleEffect.isPlaying)
            {
                _particleEffect.Stop();
            }
            if (_audioSource.isPlaying)
            {
                _audioSource.FadeOut(0.5f);
                _audioSource.time = 0f;
            }
        }
    }

    private void OnLandingGearDamaged()
    {
        _damaged = true;
        if (_particleEffect.isPlaying)
        {
            _particleEffect.Stop();
        }
        if (_audioSource.isPlaying)
        {
            _audioSource.FadeOut(0.5f);
            _audioSource.time = 0f;
        }
    }

    private void OnLandingGearRepaired()
    {
        _damaged = false;

        if (_shipDestroyed || !_gravityEnabled) return;

        if (_landed)
        {
            _particleEffect.Play();
        }
        _audioSource.FadeIn(1f);
        _audioSource.pitch = 1f;
    }

    private void OnDestroy()
    {
        ShipEnhancements.Instance.OnGravityLandingGearSwitch -= SetGravityEnabled;
        ShipEnhancements.Instance.OnGravityLandingGearInverted -= SetGravityInverted;
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        _landingGear.OnDamaged -= ctx => OnLandingGearDamaged();
        _landingGear.OnRepaired -= ctx => OnLandingGearRepaired();
        ShipEnhancements.Instance.OnEngineStateChanged -= OnEngineStateChanged;
    }
}
