using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShipEnhancements;

public class OrbitAutopilotTest : ThrusterController
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

    public delegate void AbortAutopilotEvent();
    public event AbortAutopilotEvent OnAbortAutopilot;
    public delegate void InitOrbitEvent();
    public event InitOrbitEvent OnInitOrbit;

    private OWRigidbody _owRigidbody;
    private ReferenceFrame _referenceFrame;
    private RulesetDetector _rulesetDetector;
    private ForceDetector _forceDetector;

    // debug
    private LineRenderer _targetOrbitPathRenderer;
    private LineRenderer _targetOrbitalPositionRenderer;
    private LineRenderer _positionDeltaRenderer;
    private LineRenderer _velocityDeltaRenderer;
    private LineRenderer _orbitVelocityDeltaRenderer;
    private LineRenderer _orbitPositionImpulseRenderer;

    private bool _ignoreThrustLimits;
    private float _orbitRadius;
    private float _orbitSpeed;
    private Vector3 _orbitalPlaneNormal;

    private bool _usePid = true;
    private float _kP = 0.35f;
    private float _kI = 0.0f;
    private float _kD = 1.0f;
    private Vector3 _errorIntegral = Vector3.zero;

    private Vector3 _currentPosition;
    private Vector3 _velocityRelativeToTarget;
    private float _speedTowardsTarget;
    private Vector3 _relativePosition;
    private float _maxThrust;
    private float _maxVelocityChange;
    private float _shipThrustFactor;
    private Vector3 _targetOrbitalDirection;
    private Vector3 _targetOrbitalPosition;
    private Vector3 _targetOrbitalVelocity;
    private Vector3 _deltaOrbitalVelocity;
    private float _deltaOrbitalSpeed;
    private Vector3 _deltaPositionToOrbit;
    private float _distanceToOrbit;
    private float _proximityThrottleFactor;
    private Vector3 _orbitPositionImpulse;
    private Vector3 _decelerationForce;
    private Vector3 _decelerationForceTowardsOrbit;
    private float _accelerationToMatchOrbit;
    private float _timeToDecelerate;
    private float _distanceToDecelerate;
    private Vector3 _desiredImpulse;

    public override void Awake()
    {
        base.Awake();
        _owRigidbody = this.GetRequiredComponent<OWRigidbody>();
        _rulesetDetector = this.GetRequiredComponentInChildren<RulesetDetector>();
        _forceDetector = this.GetRequiredComponentInChildren<ForceDetector>();

        _targetOrbitPathRenderer = GetDebugLineRenderer("TargetOrbitPath");
        _targetOrbitalPositionRenderer = GetDebugLineRenderer("TargetOrbitalPosition");
        _positionDeltaRenderer = GetDebugLineRenderer("PositionDelta");
        _velocityDeltaRenderer = GetDebugLineRenderer("VelocityDelta");
        _orbitVelocityDeltaRenderer = GetDebugLineRenderer("OrbitVelocityDelta");
        _orbitPositionImpulseRenderer = GetDebugLineRenderer("OrbitPositionImpulse");

        enabled = false;
    }

    private LineRenderer GetDebugLineRenderer(string objectName)
    {
        return ShipEnhancements.Instance.DebugObjects.transform.Find(objectName).GetComponent<LineRenderer>();
    }

    public void SetOrbitEnabled(bool orbit, bool ignoreThrustLimits = true)
    {
        if (!orbit || Locator.GetReferenceFrame(false) == null
            || !SELocator.GetShipResources().AreThrustersUsable())
        {
            if (enabled)
            {
                PostAutopilotOffNotification();
                enabled = false;
                _ignoreThrustLimits = false;
                ShipEnhancements.WriteDebugMessage("nothing to orbit");
            }
            return;
        }

        _referenceFrame = Locator.GetReferenceFrame(false);
        _ignoreThrustLimits = ignoreThrustLimits;
        _errorIntegral = Vector3.zero;

        var relativeVelocity = _referenceFrame.GetOWRigidBody().GetRelativeVelocity(_owRigidbody);
        var dirToReference = _referenceFrame.GetPosition() - _owRigidbody.GetWorldCenterOfMass();
        _orbitRadius = dirToReference.magnitude;
        _orbitSpeed = _referenceFrame.GetOrbitSpeed(_orbitRadius);
        _orbitalPlaneNormal = Vector3.Cross(relativeVelocity, dirToReference).normalized;

        _targetOrbitPathRenderer.transform.SetParent(_referenceFrame.GetOWRigidBody().transform);
        _targetOrbitPathRenderer.transform.localPosition = Vector3.zero;
        _targetOrbitPathRenderer.transform.localRotation = Quaternion.FromToRotation(Vector3.up, _orbitalPlaneNormal);
        _targetOrbitPathRenderer.widthMultiplier = _orbitRadius / 1000;
        _targetOrbitPathRenderer.positionCount = CirclePoints.Length;
        _targetOrbitPathRenderer.SetPositions(CirclePoints
            .Select(p => p * _orbitRadius)
            .ToArray()
        );

        enabled = true;

        OnInitOrbit?.Invoke();

        ShipNotifications.PostOrbitAutopilotActiveNotification(_orbitRadius);
    }

    public override Vector3 ReadTranslationalInput()
    {
        if (Locator.GetReferenceFrame(false) != _referenceFrame || !SELocator.GetShipResources().AreThrustersUsable())
        {
            PostAutopilotOffNotification();
            enabled = false;
            OnAbortAutopilot?.Invoke();
            ShipEnhancements.WriteDebugMessage("nothing to orbit");
            return Vector3.zero;
        }

        _currentPosition = _owRigidbody.GetWorldCenterOfMass();
        _velocityRelativeToTarget = _referenceFrame.GetOWRigidBody().GetRelativeVelocity(_owRigidbody);
        _relativePosition = _currentPosition - _referenceFrame.GetPosition();
        _speedTowardsTarget = Vector3.Dot(_velocityRelativeToTarget, -_relativePosition.normalized);

        _maxThrust = _ignoreThrustLimits
            ? _thrusterModel.GetMaxTranslationalThrust()
            : Mathf.Min(_rulesetDetector.GetThrustLimit(), _thrusterModel.GetMaxTranslationalThrust());
        _maxVelocityChange = _maxThrust * Time.fixedDeltaTime;
        _shipThrustFactor = _maxThrust / _thrusterModel.GetMaxTranslationalThrust();

        _targetOrbitalDirection = Vector3.ProjectOnPlane(_relativePosition, _orbitalPlaneNormal).normalized;
        _targetOrbitalPosition = _referenceFrame.GetPosition() + _targetOrbitalDirection * _orbitRadius;
        _targetOrbitalVelocity = Vector3.Cross(_orbitalPlaneNormal, _targetOrbitalDirection) * _orbitSpeed;
        _deltaOrbitalVelocity = _targetOrbitalVelocity - Vector3.Project(_velocityRelativeToTarget, _targetOrbitalVelocity.normalized);
        _deltaOrbitalSpeed = _deltaOrbitalVelocity.magnitude;
        _deltaPositionToOrbit = _targetOrbitalPosition - _currentPosition;
        _distanceToOrbit = _deltaPositionToOrbit.magnitude;
        _proximityThrottleFactor = Mathf.Lerp(0.05f, 1, (_distanceToOrbit + Mathf.Abs(_speedTowardsTarget)) / 1000);

        if (_usePid)
            GetPidOrbitalPositionInput();
        else
            GetNaiveOrbitalPositionInput();

        _desiredImpulse = _deltaOrbitalVelocity;
        if (_deltaOrbitalSpeed < _maxVelocityChange)
        {
            _desiredImpulse *= _deltaOrbitalSpeed / _maxVelocityChange;
        }

        _desiredImpulse += _orbitPositionImpulse;

        SetArrow(_targetOrbitalPositionRenderer, _targetOrbitalPosition, _targetOrbitalVelocity);
        SetArrow(_positionDeltaRenderer, _currentPosition, _deltaPositionToOrbit);
        SetArrow(_velocityDeltaRenderer, _currentPosition, _velocityRelativeToTarget);
        SetArrow(_orbitVelocityDeltaRenderer, _currentPosition, _deltaOrbitalVelocity);
        SetArrow(_orbitPositionImpulseRenderer, _currentPosition, _orbitPositionImpulse);

        return transform.InverseTransformDirection(_shipThrustFactor * Vector3.ClampMagnitude(_desiredImpulse, 1)
            * ShipEnhancements.Instance.ThrustModulatorLevel / 5f);
    }

    private void GetNaiveOrbitalPositionInput()
    {
        // acceleration direction to orbit
        _orbitPositionImpulse = _deltaPositionToOrbit.normalized * _proximityThrottleFactor;

        if (_speedTowardsTarget < 0) return;

        _decelerationForce = -_orbitPositionImpulse;
        _decelerationForce += _forceDetector.GetForceAcceleration();
        _decelerationForceTowardsOrbit = Vector3.Project(_decelerationForce, _deltaPositionToOrbit.normalized);
        _accelerationToMatchOrbit = _decelerationForceTowardsOrbit.magnitude;
        _timeToDecelerate = _speedTowardsTarget / _accelerationToMatchOrbit;
        _distanceToDecelerate = _speedTowardsTarget * _timeToDecelerate / 2;

        if (0.8 * _distanceToOrbit < _distanceToDecelerate)
        {
            // flip the direction of impulse, to decelerate
            _orbitPositionImpulse *= -1;
        }
    }

    private void GetPidOrbitalPositionInput()
    {
        var proportional = _deltaPositionToOrbit;
        var integral = _errorIntegral + _deltaPositionToOrbit * Time.fixedDeltaTime;
        var differential = _velocityRelativeToTarget - _targetOrbitalVelocity;

        _errorIntegral = integral;

        // _orbitPositionImpulse = Vector3.ClampMagnitude(_kP * proportional + _kI * integral - _kD * differential, 1);
        _orbitPositionImpulse = _kP * proportional + _kI * integral - _kD * differential;
    }

    private void PostAutopilotOffNotification()
    {
        ShipNotifications.RemoveOrbitAutopilotActiveNotification();
        if (enabled)
            ShipNotifications.PostOrbitAutopilotDisabledNotification();
        else
            ShipNotifications.PostOrbitAutopilotNoTargetNotification();
    }

    public override Vector3 ReadRotationalInput()
    {
        return Vector3.zero;
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
}