using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ShipEnhancements;

public class PidAutopilot : ThrusterController
{
    private static readonly Vector3[] CirclePoints = Enumerable.Range(0, 128)
        .Select(i => Quaternion.AngleAxis(i * 360f / 128, Vector3.up) * Vector3.right)
        .ToArray();

    private static readonly Vector3[] ArrowHeadPoints =
        new Vector2[]
            {
                new(0, -1),
                new(-0.5f, -1),
                new(0, 0),
                new(0.5f, -1),
                new(0, -1),
            }
            .Select(p => new Vector3(p.x, 0, p.y))
            .ToArray();

    public delegate void AbortOrbitEvent();
    public event AbortOrbitEvent OnAbortOrbit;
    public delegate void InitOrbitEvent();
    public event InitOrbitEvent OnInitOrbit;
    public delegate void AbortHoldPositionEvent();
    public event AbortHoldPositionEvent OnAbortHoldPosition;
    public delegate void InitHoldPositionEvent();
    public event InitHoldPositionEvent OnInitHoldPosition;

    private OWRigidbody _owRigidbody;
    private ReferenceFrame _referenceFrame;
    private RulesetDetector _rulesetDetector;
    private ForceDetector _forceDetector;
    private ShipAudioController _shipAudio;

    // debug
    private bool _debugEnabled = false;
    private LineRenderer _targetOrbitPathRenderer;
    private LineRenderer _targetOrbitalPositionRenderer;
    private LineRenderer _positionDeltaRenderer;
    private LineRenderer _velocityDeltaRenderer;
    private LineRenderer _orbitVelocityDeltaRenderer;
    private LineRenderer _orbitPositionImpulseRenderer;

    private bool _ignoreThrustLimits;
    private PidMode _mode = PidMode.Orbit;
    private float _orbitRadius;
    private float _orbitSpeed;
    private Vector3 _orbitalPlaneNormal;
    private Vector3 _holdPosition;
    private bool _localHold = false;
    private PidComputations _comps;

    private bool _usePid = true;
    private float _kP = 0.35f;
    private float _kI = 0.0f;
    private float _kD = 0.9f;
    private Vector3 _errorIntegral = Vector3.zero;

    public override void Awake()
    {
        base.Awake();
        _owRigidbody = this.GetRequiredComponent<OWRigidbody>();
        _rulesetDetector = this.GetRequiredComponentInChildren<RulesetDetector>();
        _forceDetector = this.GetRequiredComponentInChildren<ForceDetector>();
        _shipAudio = GetComponentInChildren<ShipAudioController>();

        if (_debugEnabled)
        {
            _targetOrbitPathRenderer = GetDebugLineRenderer("TargetOrbitPath");
            _targetOrbitalPositionRenderer = GetDebugLineRenderer("TargetOrbitalPosition");
            _positionDeltaRenderer = GetDebugLineRenderer("PositionDelta");
            _velocityDeltaRenderer = GetDebugLineRenderer("VelocityDelta");
            _orbitVelocityDeltaRenderer = GetDebugLineRenderer("OrbitVelocityDelta");
            _orbitPositionImpulseRenderer = GetDebugLineRenderer("OrbitPositionImpulse");
        }

        enabled = false;
    }

    private LineRenderer GetDebugLineRenderer(string objectName)
    {
        return ShipEnhancements.Instance.DebugObjects.transform.Find(objectName).GetComponent<LineRenderer>();
    }

    public void SetAutopilotActive(bool active, PidMode mode = PidMode.Orbit, bool ignoreThrustLimits = true)
    {
        ShipEnhancements.WriteDebugMessage($"set active: {active}");
        if (!active || !CanAutopilot(mode == PidMode.Orbit))
        {
            if (enabled)
            {
                PostAutopilotOffNotification();
                enabled = false;
                _referenceFrame = null;
                _ignoreThrustLimits = false;
                if (_mode == PidMode.Orbit)
                {
                    OnAbortOrbit?.Invoke();
                }
                else
                {
                    OnAbortHoldPosition?.Invoke();
                }
                ShipEnhancements.WriteDebugMessage("nothing to orbit");
            }
            return;
        }
        else if (active)
        {
            _mode = mode;
            if (enabled)
            {
                ShipNotifications.RemoveOrbitAutopilotActiveNotification();
                ShipNotifications.RemoveHoldPositionAutopilotNotification();
            }
        }

        _referenceFrame = Locator.GetReferenceFrame(false);
        _ignoreThrustLimits = ignoreThrustLimits;
        _localHold = _mode == PidMode.HoldPosition && SELocator.GetShipDetector().GetComponent<RulesetDetector>()?.GetPlanetoidRuleset();
        _errorIntegral = Vector3.zero;

        var relativeVelocity = _referenceFrame.GetOWRigidBody().GetRelativeVelocity(_owRigidbody);
        var dirToReference = _referenceFrame.GetPosition() - _owRigidbody.GetWorldCenterOfMass();
        if (_localHold)
        {
            _holdPosition = _referenceFrame.GetOWRigidBody().transform.InverseTransformPoint(_owRigidbody.GetWorldCenterOfMass());
            ShipEnhancements.WriteDebugMessage(_holdPosition);
        }
        else
        {
            _holdPosition = -dirToReference;
        }
        
        _orbitRadius = dirToReference.magnitude;
        _orbitSpeed = _referenceFrame.GetOrbitSpeed(_orbitRadius);
        _orbitalPlaneNormal = Vector3.Cross(relativeVelocity, dirToReference).normalized;

        if (_debugEnabled)
        {
            _targetOrbitPathRenderer.transform.SetParent(_referenceFrame.GetOWRigidBody().transform);
            _targetOrbitPathRenderer.transform.localPosition = Vector3.zero;
            _targetOrbitPathRenderer.transform.localRotation =
                Quaternion.FromToRotation(Vector3.up, _orbitalPlaneNormal);
            _targetOrbitPathRenderer.widthMultiplier = _orbitRadius / 1000;
            _targetOrbitPathRenderer.positionCount = CirclePoints.Length;
            _targetOrbitPathRenderer.SetPositions(CirclePoints
                .Select(p => p * _orbitRadius)
                .ToArray()
            );
        }

        enabled = true;

        if (_mode == PidMode.Orbit)
        {
            OnInitOrbit?.Invoke();
        }
        else
        {
            OnInitHoldPosition?.Invoke();
        }

        _shipAudio.PlayAutopilotOn();
        if (_mode == PidMode.Orbit)
            ShipNotifications.PostOrbitAutopilotActiveNotification(_orbitRadius);
        else
            ShipNotifications.PostHoldPositionAutopilotNotification();
    }

    public override Vector3 ReadTranslationalInput()
    {
        if (!CanAutopilot(_mode == PidMode.Orbit))
        {
            PostAutopilotOffNotification();
            enabled = false;
            _referenceFrame = null;
            if (_mode == PidMode.Orbit)
            {
                OnAbortOrbit?.Invoke();
            }
            else
            {
                OnAbortHoldPosition?.Invoke();
            }
            ShipEnhancements.WriteDebugMessage("nothing to orbit");
            return Vector3.zero;
        }

        _comps = CalculateCurrentState();

        if (_mode == PidMode.HoldPosition)
        {
            _comps.TargetOrbitalVelocity = Vector3.zero;
        }

        ComputePidPositionInput(_comps);

        var desiredImpulse = Vector3.zero;
        if (_mode == PidMode.Orbit)
        {
            desiredImpulse = _comps.DeltaOrbitalVelocity;
            if (_comps.DeltaOrbitalSpeed < _comps.MaxVelocityChange)
            {
                desiredImpulse *= _comps.DeltaOrbitalSpeed / _comps.MaxVelocityChange;
            }
        }

        desiredImpulse += _comps.PositionImpulse;

        if (_debugEnabled)
        {
            SetArrow(_targetOrbitalPositionRenderer, _comps.TargetOrbitalPosition, _comps.TargetOrbitalVelocity);
            SetArrow(_positionDeltaRenderer, _comps.CurrentPosition, _comps.DeltaPosition);
            SetArrow(_velocityDeltaRenderer, _comps.CurrentPosition, _comps.VelocityRelativeToTarget);
            SetArrow(_orbitVelocityDeltaRenderer, _comps.CurrentPosition, _comps.DeltaOrbitalVelocity);
            SetArrow(_orbitPositionImpulseRenderer, _comps.CurrentPosition, _comps.PositionImpulse);
        }

        return transform.InverseTransformDirection(_comps.ShipThrustFactor * Vector3.ClampMagnitude(desiredImpulse, 1) * ShipEnhancements.Instance.ThrustModulatorFactor);
    }

    private PidComputations CalculateCurrentState()
    {
        var comps = new PidComputations();
        
        comps.CurrentPosition = _owRigidbody.GetWorldCenterOfMass();
        comps.VelocityRelativeToTarget = _referenceFrame.GetOWRigidBody().GetRelativeVelocity(_owRigidbody);
        comps.RelativePosition = comps.CurrentPosition - _referenceFrame.GetPosition();
        comps.SpeedTowardsTarget = Vector3.Dot(comps.VelocityRelativeToTarget, -comps.RelativePosition.normalized);

        comps.MaxThrust = _ignoreThrustLimits
            ? _thrusterModel.GetMaxTranslationalThrust()
            : Mathf.Min(_rulesetDetector.GetThrustLimit(), _thrusterModel.GetMaxTranslationalThrust());
        comps.MaxVelocityChange = comps.MaxThrust * Time.fixedDeltaTime;
        comps.ShipThrustFactor = comps.MaxThrust / _thrusterModel.GetMaxTranslationalThrust();

        comps.TargetOrbitalDirection = Vector3.ProjectOnPlane(comps.RelativePosition, _orbitalPlaneNormal).normalized;
        comps.TargetOrbitalPosition = _referenceFrame.GetPosition() + comps.TargetOrbitalDirection * _orbitRadius;
        comps.TargetHoldPosition = comps.TargetOrbitalPosition;
        if (_mode == PidMode.HoldPosition)
        {
            comps.TargetHoldPosition = _referenceFrame.GetPosition();
            if (_localHold)
            {
                comps.TargetHoldPosition = _referenceFrame.GetOWRigidBody().transform.TransformPoint(_holdPosition);
            }
            else
            {
                comps.TargetHoldPosition += _holdPosition;
            }
        }

        comps.TargetOrbitalVelocity = Vector3.Cross(_orbitalPlaneNormal, comps.TargetOrbitalDirection) * _orbitSpeed;
        comps.DeltaOrbitalVelocity = comps.TargetOrbitalVelocity - Vector3.Project(comps.VelocityRelativeToTarget, comps.TargetOrbitalVelocity.normalized);
        comps.DeltaOrbitalSpeed = comps.DeltaOrbitalVelocity.magnitude;
        comps.DeltaPosition = comps.TargetHoldPosition - comps.CurrentPosition;
        
        comps.DistanceToOrbit = comps.DeltaPosition.magnitude;
        comps.ProximityThrottleFactor = Mathf.Lerp(0.05f, 1, (comps.DistanceToOrbit + Mathf.Abs(comps.SpeedTowardsTarget)) / 1000);

        return comps;
    }

    private void ComputeNaiveOrbitalPositionInput(PidComputations comps)
    {
        // acceleration direction to orbit
        comps.PositionImpulse = comps.DeltaPosition.normalized * comps.ProximityThrottleFactor;

        if (comps.SpeedTowardsTarget < 0) return;

        comps.DecelerationForce = -comps.PositionImpulse;
        comps.DecelerationForce += _forceDetector.GetForceAcceleration();
        comps.DecelerationForceTowardsOrbit = Vector3.Project(comps.DecelerationForce, comps.DeltaPosition.normalized);
        comps.AccelerationToMatchOrbit = comps.DecelerationForceTowardsOrbit.magnitude;
        comps.TimeToDecelerate = comps.SpeedTowardsTarget / comps.AccelerationToMatchOrbit;
        comps.DistanceToDecelerate = comps.SpeedTowardsTarget * comps.TimeToDecelerate / 2;

        if (0.8 * comps.DistanceToOrbit < comps.DistanceToDecelerate)
        {
            // flip the direction of impulse, to decelerate
            comps.PositionImpulse *= -1;
        }
    }

    private void ComputePidPositionInput(PidComputations comps)
    {
        var proportional = comps.DeltaPosition;
        // var integral = _errorIntegral + comps.DeltaPositionToOrbit * Time.fixedDeltaTime;
        var differential = comps.VelocityRelativeToTarget - comps.TargetOrbitalVelocity;

        // _errorIntegral = integral;

        // _orbitPositionImpulse = Vector3.ClampMagnitude(_kP * proportional + _kI * integral - _kD * differential, 1);
        comps.PositionImpulse = _kP * proportional /*+ _kI * integral*/ - _kD * differential;
    }

    private void PostAutopilotOffNotification()
    {
        _shipAudio.PlayAutopilotOff();
        ShipNotifications.RemoveOrbitAutopilotActiveNotification();
        ShipNotifications.RemoveHoldPositionAutopilotNotification();
        if (enabled)
        {
            if (_mode == PidMode.Orbit)
            {
                ShipNotifications.PostOrbitAutopilotDisabledNotification();
            }
            else
            {
                ShipNotifications.PostHoldPositionAutopilotDisabledNotification();
            }
        }
        else
        {
            ShipNotifications.PostOrbitAutopilotNoTargetNotification();
        }
    }

    public override Vector3 ReadRotationalInput()
    {
        return Vector3.zero;
    }

    private bool CanAutopilot(bool checkCorrectRefFrame)
    {
        if (Locator.GetReferenceFrame(false) == null 
            || Locator.GetReferenceFrame() == SELocator.GetShipBody().GetReferenceFrame()
            || (_referenceFrame != null 
            && Locator.GetReferenceFrame(false) != _referenceFrame)) return false;

        //var hasCorrectRefFrame = Locator.GetReferenceFrame(false) == _referenceFrame;
        var thrustersUsable = SELocator.GetShipResources().AreThrustersUsable();
        var refFrameHasGravity = Locator.GetReferenceFrame(false).GetOWRigidBody().GetAttachedGravityVolume() != null;
        return (!checkCorrectRefFrame || refFrameHasGravity) && thrustersUsable;
    }

    private void SetArrow(LineRenderer renderer, Vector3 position, Vector3 vector)
    {
        var arrowLength = vector.magnitude;
        renderer.transform.SetPositionAndRotation(
            position,
            Quaternion.FromToRotation(Vector3.forward, vector.normalized)
        );
        renderer.widthMultiplier = arrowLength / 24;
        renderer.positionCount = ArrowHeadPoints.Length + 1;
        renderer.SetPositions(ArrowHeadPoints
            .Select(p => arrowLength * (p / 8 + Vector3.forward))
            .Concat([Vector3.zero])
            .ToArray()
        );
    }

    public PidMode GetCurrentMode()
    {
        return _mode;
    }

    private record PidComputations
    {
        public Vector3 CurrentPosition;
        public Vector3 VelocityRelativeToTarget;
        public float SpeedTowardsTarget;
        public Vector3 RelativePosition;
        public float MaxThrust;
        public float MaxVelocityChange;
        public float ShipThrustFactor;
        public Vector3 TargetOrbitalDirection;
        public Vector3 TargetOrbitalPosition;
        public Vector3 TargetHoldPosition;
        public Vector3 TargetOrbitalVelocity;
        public Vector3 DeltaOrbitalVelocity;
        public float DeltaOrbitalSpeed;
        public Vector3 DeltaPosition;
        public Vector3 DeltaPositionToHold;
        public float DistanceToOrbit;
        public float ProximityThrottleFactor;
        public Vector3 PositionImpulse;
        public Vector3 DecelerationForce;
        public Vector3 DecelerationForceTowardsOrbit;
        public float AccelerationToMatchOrbit;
        public float TimeToDecelerate;
        public float DistanceToDecelerate;
    }
}


public enum PidMode
{
    HoldPosition,
    Orbit,
}
