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
    private bool _inverted = true;
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

    private void FixedUpdate()
    {
        if (_shipDestroyed || _landingGear.isDamaged || !_gravityEnabled || !ShipEnhancements.Instance.engineOn)
        {
            return;
        }

        if (_inverted)
        {
            if (_landed != _weakRepelVol.IsRepelling())
            {
                _landed = _weakRepelVol.IsRepelling();
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
                if (_baseRepelVol.IsRepelling())
                {
                    mult = 1f;
                }
                else if (_strongRepelVol.IsRepelling())
                {
                    mult = 0.75f;
                }
                else if (_midRepelVol.IsRepelling())
                {
                    mult = 0.5f;
                }
                SELocator.GetShipBody().AddAcceleration(transform.up * _gravityMagnitude * mult);
            }
        }
    }

    /*private void OnTriggerStay(Collider hitCollider)
    {
        if (OWLayerMask.IsLayerInMask(hitCollider.gameObject.layer, OWLayerMask.physicalMask)
            || hitCollider.GetComponent<FluidVolume>()?.GetFluidType() == FluidVolume.Type.WATER)
        {
            if (_shipDestroyed || _landingGear.isDamaged || !_gravityEnabled || !ShipEnhancements.Instance.engineOn
                *//*|| _thrusterModel.GetLocalAcceleration().y > 0f*//*)
            {
                return;
            }

            if (hitCollider.attachedRigidbody != null)
            {
                if (hitCollider.attachedRigidbody.isKinematic)
                {
                    float mult = 1f;
                    if (_strongRepelVol.IsRepelling())
                    {
                        mult = 2.5f;
                    }
                    else if (_midRepelVol.IsRepelling())
                    {
                        mult = 1.5f;
                    }
                    SELocator.GetShipBody().AddAcceleration(transform.up * _gravityMagnitude * mult);
                }
                else
                {
                    SELocator.GetShipBody().AddAcceleration(-transform.up * (_gravityMagnitude / 10) * (1 / SELocator.GetShipBody().GetMass()));
                    hitCollider.attachedRigidbody.GetAttachedOWRigidbody().AddAcceleration(transform.up * (_gravityMagnitude / 10) * (1 / hitCollider.attachedRigidbody.mass));
                }
            }
        }
    }*/

    /*private void OnTriggerEnter(Collider hitCollider)
    {
        if (OWLayerMask.IsLayerInMask(hitCollider.gameObject.layer, OWLayerMask.physicalMask)
            || hitCollider.GetComponent<FluidVolume>()?.GetFluidType() == FluidVolume.Type.WATER)
        {
            if (hitCollider.attachedRigidbody != null)
            {
                _landed = true;
                if (!_shipDestroyed && !_damaged && _gravityEnabled)
                {
                    _particleEffect.Play();
                }
            }
        }
    }

    private void OnTriggerExit(Collider hitCollider)
    {
        if (OWLayerMask.IsLayerInMask(hitCollider.gameObject.layer, OWLayerMask.physicalMask)
            || hitCollider.GetComponent<FluidVolume>()?.GetFluidType() == FluidVolume.Type.WATER)
        {
            if (hitCollider.attachedRigidbody != null)
            {
                _landed = false;
                _particleEffect.Stop();
            }
        }
    }*/

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
        GlobalMessenger.RemoveListener("ShipSystemFailure", OnShipSystemFailure);
        _landingGear.OnDamaged -= ctx => OnLandingGearDamaged();
        _landingGear.OnRepaired -= ctx => OnLandingGearRepaired();
        ShipEnhancements.Instance.OnEngineStateChanged -= OnEngineStateChanged;
    }
}
