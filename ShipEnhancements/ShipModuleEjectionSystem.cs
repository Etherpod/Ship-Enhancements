using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

[RequireComponent(typeof(InteractVolume))]
public class ShipModuleEjectionSystem : MonoBehaviour
{
    [SerializeField]
    private EjectableModule _targetModule;
    [SerializeField]
    private float _ejectImpulse = 5f;
    [SerializeField]
    private Transform _cover;
    [SerializeField]
    private float _coverMoveTime = 1f;
    [SerializeField]
    private float _secondPressDelay = 0.25f;

    public enum EjectableModule
    {
        Cockpit,
        Supplies,
        Engine,
        LandingGear
    }

    private SingleInteractionVolume _interactVolume;
    private ShipAudioController _audioController;
    private OWRigidbody _shipBody;
    private float _pressTime;
    private float _coverT;
    private bool _raising;
    private bool _ejectPrimed;
    private bool _ejectPressed;
    private ShipDetachableModule _detachableModule;
    private ShipLandingGear _landingGear;
    private Vector3 _ejectDirection;

    private void Awake()
    {
        _interactVolume = GetComponent<SingleInteractionVolume>();
        _shipBody = this.GetAttachedOWRigidbody(false);
        _interactVolume.OnPressInteract += OnPressInteract;
        _interactVolume.OnLoseFocus += OnLoseFocus;
    }

    private void Start()
    {
        _audioController = Locator.GetShipTransform().GetComponentInChildren<ShipAudioController>();
        _interactVolume.ChangePrompt(UITextType.ShipEjectPrompt);

        switch (_targetModule)
        {
            case EjectableModule.Cockpit:
                _detachableModule = SELocator.GetShipTransform().Find("Module_Cockpit").GetComponent<ShipDetachableModule>();
                _ejectDirection = Vector3.forward;
                break;
            case EjectableModule.Supplies:
                _detachableModule = SELocator.GetShipTransform().Find("Module_Supplies").GetComponent<ShipDetachableModule>();
                _ejectDirection = Vector3.right;
                break;
            case EjectableModule.Engine:
                _detachableModule = SELocator.GetShipTransform().Find("Module_Engine").GetComponent<ShipDetachableModule>();
                _ejectDirection = Vector3.left;
                break;
            case EjectableModule.LandingGear:
                _landingGear = SELocator.GetShipTransform().Find("Module_LandingGear").GetComponent<ShipLandingGear>();
                _ejectDirection = Vector3.down;
                break;
        }
    }

    private void OnDestroy()
    {
        _interactVolume.OnPressInteract -= OnPressInteract;
        _interactVolume.OnLoseFocus -= OnLoseFocus;
    }

    private void Update()
    {
        if (Time.time >= _pressTime + _secondPressDelay && _raising)
        {
            _interactVolume.ChangePrompt(UITextType.ShipEjectFinalPrompt);
            _ejectPrimed = true;
        }
        if (_raising)
        {
            _coverT = Mathf.Clamp01(_coverT + Time.deltaTime / _coverMoveTime);
        }
        else
        {
            _coverT = Mathf.Clamp01(_coverT - Time.deltaTime / _coverMoveTime);
        }
        _cover.localEulerAngles = new Vector3(0f, Mathf.SmoothStep(0f, -160f, _coverT), 0f);
        if (_coverT <= 0f && !_raising && !_ejectPressed)
        {
            enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (_ejectPressed)
        {
            if (_landingGear != null)
            {
                List<OWRigidbody> legs = [];
                foreach (ShipDetachableLeg leg in _landingGear.GetLegs())
                {
                    legs.Add(leg.Detach());
                }

                //_shipBody.transform.position -= _shipBody.transform.TransformVector(_ejectDirection);
                float num = _ejectImpulse;
                if (Locator.GetShipDetector().GetComponent<ShipFluidDetector>().InOceanBarrierZone())
                {
                    MonoBehaviour.print("Ship in ocean barrier zone, reducing eject impulse.");
                    num = 1f;
                }
                _shipBody.AddLocalImpulse(-_ejectDirection * num / 2f);
                foreach (OWRigidbody leg in legs)
                {
                    Vector3 toShip = leg.transform.position - _shipBody.transform.position;
                    leg.AddLocalImpulse(-toShip.normalized * num);
                }

                if (ShipEnhancements.InMultiplayer)
                {
                    foreach (uint id in ShipEnhancements.PlayerIDs)
                    {
                        ShipEnhancements.QSBCompat.SendDetachLandingGear(id, _ejectImpulse);
                    }
                }
            }
            else
            {
                OWRigidbody owrigidbody = _detachableModule.Detach();
                _shipBody.transform.position -= _shipBody.transform.TransformVector(_ejectDirection);
                float num = _ejectImpulse;
                if (Locator.GetShipDetector().GetComponent<ShipFluidDetector>().InOceanBarrierZone())
                {
                    MonoBehaviour.print("Ship in ocean barrier zone, reducing eject impulse.");
                    num = 1f;
                }
                _shipBody.AddLocalImpulse(-_ejectDirection * num);
                owrigidbody.AddLocalImpulse(_ejectDirection * num);
            }

            _audioController.PlayEject();
            RumbleManager.PulseEject();
            enabled = false;
        }
    }

    private void OnPressInteract()
    {
        if (!CanEject()) return;

        if (_ejectPrimed)
        {
            _ejectPressed = true;
            //Achievements.Earn(Achievements.Type.WHATS_THIS_BUTTON);
        }
        else
        {
            _raising = true;
            _audioController.PlayRaiseEjectCover();
        }
        _pressTime = Time.time;
        enabled = true;
    }

    public bool CanEject()
    {
        if (_landingGear != null)
        {
            bool found = false;
            foreach (ShipDetachableLeg leg in _landingGear.GetLegs())
            {
                if (!leg.isDetached)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        else if (_detachableModule.isDetached)
        {
            return false;
        }

        return true;
    }

    public EjectableModule GetEjectType()
    {
        return _targetModule;
    }

    public void Eject()
    {
        _ejectPressed = true;
        enabled = true;
    }

    private void OnLoseFocus()
    {
        _raising = false;
        _ejectPrimed = false;
        _interactVolume.ChangePrompt(UITextType.ShipEjectPrompt);
    }
}
