using System.Linq;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ShipWarpCoreController : CockpitInteractible
{
    [SerializeField]
    private Transform _buttonTransform;
    [SerializeField]
    private SingularityWarpEffect _warpEffect;
    [SerializeField]
    private Transform _shipPivot;
    [SerializeField]
    private Transform _cockpitPivot;

    private OWRigidbody _shipBody;
    private ShipWarpCoreReceiver _receiver;
    private bool _warpingWithPlayer = false;
    private readonly float _warpLength = 1f;
    private bool _warping = false;
    private bool _damaged = false;
    private bool _pressed = false;
    private float _buttonOffset = -0.121f;
    private GravityCannonController _brittleHollowCannon;
    private GravityCannonController _emberTwinCannon;
    private GravityCannonController _targetCannon;
    private Transform _randomDestination;

    private readonly string _brittleHollowCannonEntryID = "BH_GRAVITY_CANNON";
    private readonly string _emberTwinCannonEntryID = "CT_GRAVITY_CANNON";

    private void Start()
    {
        _shipBody = SELocator.GetShipBody() ?? FindObjectOfType<ShipBody>();
        _brittleHollowCannon = Locator.GetGravityCannon(NomaiShuttleController.ShuttleID.BrittleHollowShuttle);
        _emberTwinCannon = Locator.GetGravityCannon(NomaiShuttleController.ShuttleID.HourglassShuttle);

        GlobalMessenger<OWRigidbody>.AddListener("ShipCockpitDetached", OnShipCockpitDetached);

        _randomDestination = new GameObject("ShipRandomWarpDestination").transform;
        if (Locator.GetAstroObject(AstroObject.Name.Sun) != null)
        {
            _randomDestination.parent = Locator.GetAstroObject(AstroObject.Name.Sun).transform;
        }

        _interactReceiver.ChangePrompt("Activate Return Warp");
        _warpEffect.transform.localPosition = _shipPivot.localPosition;
    }

    protected override void OnPressInteract()
    {
        _buttonTransform.localPosition = new Vector3(0, _buttonOffset, 0);
        _pressed = true;

        ActivateWarp();
        SendWarpMessage();
    }

    protected override void OnReleaseInteract()
    {
        _buttonTransform.localPosition = Vector3.zero;
        _pressed = false;
        if (_warping)
        {
            _interactReceiver.DisableInteraction();
        }
        else
        {
            _interactReceiver.ResetInteraction();
        }
    }

    protected override void OnLoseFocus()
    {
        base.OnLoseFocus();
        if (_pressed)
        {
            OnReleaseInteract();
        }
    }

    public void SendWarpMessage()
    {
        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                string cannonID = "";
                if (_targetCannon == _brittleHollowCannon)
                {
                    cannonID = _brittleHollowCannonEntryID;
                }
                else if (_targetCannon == _emberTwinCannon)
                {
                    cannonID = _emberTwinCannonEntryID;
                }
                ShipEnhancements.QSBCompat.SendActivateWarp(id, _warpingWithPlayer, cannonID, _randomDestination.position);
            }
        }
    }

    private void WarpShip()
    {
        if (_warpingWithPlayer)
        {
            ShipEnhancements.WriteDebugMessage("Unsubscribed");
            _warpEffect.singularityController.OnCreation -= WarpShip;
            _warpEffect.singularityController.CollapseImmediate();
        }
        else
        {
            ShipEnhancements.WriteDebugMessage("Unsubscribed 2");
            _warpEffect.OnWarpComplete -= WarpShip;
            _receiver.PlayRecallEffect(_warpLength, _warpingWithPlayer);
        }

        if (_targetCannon != null && !_damaged)
        {
            _receiver.SetGravityCannonSocket(_targetCannon._shuttleSocket);
        }
        else if (_damaged && !SELocator.GetShipDamageController().IsSystemFailed() && (float)shipDamageMultiplier.GetProperty() > 0f)
        {
            ApplyWarpDamage();
        }

        _receiver.WarpBodyToReceiver(_shipBody, _warpingWithPlayer);
        _interactReceiver.EnableInteraction();
        _warping = false;
    }

    private void OnShipCockpitDetached(OWRigidbody body)
    {
        _shipBody = body;
        _warpEffect.transform.localPosition = _cockpitPivot.localPosition;
        if (_receiver != null)
        {
            _receiver.OnCockpitDetached(_cockpitPivot);
        }
    }

    public void SetReceiver(ShipWarpCoreReceiver receiver)
    {
        _receiver = receiver;
    }

    public void ActivateWarpRemote(bool playerInShip, string targetCannonEntryID, Vector3 randomPos)
    {
        if (_receiver == null || !_receiver.isActiveAndEnabled) return;

        if (targetCannonEntryID == _brittleHollowCannonEntryID)
        {
            _targetCannon = _brittleHollowCannon;
        }
        else if (targetCannonEntryID == _emberTwinCannonEntryID)
        {
            _targetCannon = _emberTwinCannon;
        }
        else
        {
            _targetCannon = null;
            _receiver.SetGravityCannonSocket(null);
        }

        if (_damaged)
        {
            _randomDestination.position = randomPos;
            _receiver.SetCustomDestination(_randomDestination);
        }

        _interactReceiver.DisableInteraction();

        if (playerInShip)
        {
            if (PlayerState.InBrambleDimension())
            {
                PlayerFogWarpDetector detector = Locator.GetPlayerDetector().GetComponent<PlayerFogWarpDetector>();
                FogWarpVolume[] volumes = detector._warpVolumes.ToArray();
                foreach (FogWarpVolume volume in volumes)
                {
                    detector.UntrackFogWarpVolume(volume);
                }
            }

            _warpingWithPlayer = true;
            _warpEffect.singularityController.OnCreation += WarpShip;
            _warpEffect.singularityController.Create();
            _receiver.PlayRecallEffect(_warpLength, _warpingWithPlayer);
        }
        else
        {
            if (!SELocator.GetShipDamageController().IsSystemFailed())
            {
                HatchController hatch = _shipBody.GetComponentInChildren<HatchController>();
                hatch._triggerVolume.SetTriggerActivation(false);
                hatch.CloseHatch();
                _shipBody.GetComponentInChildren<ShipTractorBeamSwitch>().DeactivateTractorBeam();
            }

            if ((bool)funnySounds.GetProperty())
            {
                _warpEffect._singularity._owOneShotSource.PlayOneShot(ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/tube_in.ogg"), 0.5f);
            }

            _warpingWithPlayer = false;
            _warpEffect.OnWarpComplete += WarpShip;
            _warpEffect.WarpObjectOut(_warpLength);
        }

        if ((bool)extraNoise.GetProperty())
        {
            SELocator.GetShipTransform().GetComponentInChildren<ShipNoiseMaker>()._noiseRadius = 600f;
        }

        _warping = true;
    }

    public void ActivateWarp()
    {
        if (_receiver == null || !_receiver.isActiveAndEnabled) return;

        if (ShipLogEntryHUDMarker.s_entryLocationID == _brittleHollowCannonEntryID 
            || Locator.GetReferenceFrame(true)?.GetOWRigidBody() == Locator.GetAstroObject(AstroObject.Name.BrittleHollow)?.GetOWRigidbody())
        {
            _targetCannon = _brittleHollowCannon;
        }
        else if (ShipLogEntryHUDMarker.s_entryLocationID == _emberTwinCannonEntryID
            || Locator.GetReferenceFrame(true)?.GetOWRigidBody() == Locator.GetAstroObject(AstroObject.Name.CaveTwin)?.GetOWRigidbody())
        {
            _targetCannon = _emberTwinCannon;
        }
        else
        {
            _targetCannon = null;
            _receiver.SetGravityCannonSocket(null);
        }

        if (_damaged)
        {
            _randomDestination.transform.localPosition = Random.insideUnitSphere * 20000f;
            _receiver.SetCustomDestination(_randomDestination);
        }

        if (IsShipOccupied())
        {
            ShipEnhancements.WriteDebugMessage("Ship occupied");
            if (PlayerState.InBrambleDimension())
            {
                PlayerFogWarpDetector detector = Locator.GetPlayerDetector().GetComponent<PlayerFogWarpDetector>();
                FogWarpVolume[] volumes = detector._warpVolumes.ToArray();
                foreach (FogWarpVolume volume in volumes)
                {
                    detector.UntrackFogWarpVolume(volume);
                }
            }

            _warpingWithPlayer = true;
            _warpEffect.singularityController.OnCreation += WarpShip;
            _warpEffect.singularityController.Create();
            _receiver.PlayRecallEffect(_warpLength, _warpingWithPlayer);
        }
        else
        {
            if (!SELocator.GetShipDamageController().IsSystemFailed())
            {
                HatchController hatch = _shipBody.GetComponentInChildren<HatchController>();
                hatch._triggerVolume.SetTriggerActivation(false);
                if (hatch._hatch.localRotation == hatch._hatchOpenedQuaternion)
                {
                    hatch.CloseHatch();
                }
                _shipBody.GetComponentInChildren<ShipTractorBeamSwitch>().DeactivateTractorBeam();
            }

            if ((bool)funnySounds.GetProperty())
            {
                _warpEffect._singularity._owOneShotSource.PlayOneShot(ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/tube_in.ogg"), 0.5f);
            }

            _warpingWithPlayer = false;
            _warpEffect.OnWarpComplete += WarpShip;
            _warpEffect.WarpObjectOut(_warpLength);
        }

        if ((bool)extraNoise.GetProperty())
        {
            SELocator.GetShipTransform().GetComponentInChildren<ShipNoiseMaker>()._noiseRadius = 600f;
        }

        _warping = true;
    }

    public bool IsWarping()
    {
        return _warping;
    }

    private bool IsShipOccupied()
    {
        if (ShipEnhancements.InMultiplayer)
        {
            return ShipEnhancements.QSBInteraction.GetPlayersInShip() > 0
                || ShipEnhancements.QSBInteraction.FlightConsoleOccupied();
        }
        else
        {
            return PlayerState.IsInsideShip() || PlayerState.AtFlightConsole();
        }
    }

    private void ApplyWarpDamage()
    {
        if (ShipEnhancements.InMultiplayer && !ShipEnhancements.QSBAPI.GetIsHost()) return;

        ShipComponent[] components = SELocator.GetShipDamageController()._shipComponents
            .Where((component) => component.repairFraction == 1f && !component.isDamaged).ToArray();
        if (components.Length > 0 && Random.value < 0.3f)
        {
            components[Random.Range(0, components.Length)].SetDamaged(true);
        }
        else
        {
            ShipHull[] hulls = SELocator.GetShipDamageController()._shipHulls.Where((hull) => hull.integrity > 0f).ToArray();
            ShipHull targetHull = hulls[Random.Range(0, hulls.Length)];

            bool wasDamaged = targetHull._damaged;
            targetHull._damaged = true;
            targetHull._integrity = Mathf.Max(0f, targetHull._integrity - Random.Range(0.05f, 0.15f) * (float)shipDamageMultiplier.GetProperty());
            var eventDelegate1 = (System.MulticastDelegate)typeof(ShipHull).GetField("OnDamaged",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public).GetValue(targetHull);
            if (eventDelegate1 != null)
            {
                foreach (var handler in eventDelegate1.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, [targetHull]);
                }
            }
            if (targetHull._damageEffect != null)
            {
                targetHull._damageEffect.SetEffectBlend(1f - targetHull._integrity);
            }

            if (ShipEnhancements.InMultiplayer)
            {
                ShipEnhancements.QSBInteraction.SetHullDamaged(targetHull, !wasDamaged);
            }
        }
    }

    public void SetDamaged(bool damaged)
    {
        _damaged = damaged;
        if (!_damaged)
        {
            _receiver.SetCustomDestination(null);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GlobalMessenger<OWRigidbody>.RemoveListener("ShipCockpitDetached", OnShipCockpitDetached);
    }
}
