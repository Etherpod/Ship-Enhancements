using UnityEngine;

namespace ShipEnhancements;

public class OrbitAutopilotTest : ThrusterController
{
    private OWRigidbody _owRigidbody;
    private ReferenceFrame _referenceFrame;
    private RulesetDetector _rulesetDetector;
    private ForceDetector _forceDetector;
    
    private bool _ignoreThrustLimits;
    private float _orbitRadius;
    private float _orbitSpeed;
    private Vector3 _orbitalPlaneNormal;

    public override void Awake()
    {
        base.Awake();
        _owRigidbody = this.GetRequiredComponent<OWRigidbody>();
        _rulesetDetector = this.GetRequiredComponentInChildren<RulesetDetector>();
        _forceDetector = this.GetRequiredComponentInChildren<ForceDetector>();
        enabled = false;
    }

    public void ToggleMaintainOrbit(bool ignoreThrustLimits = true)
    {
        if (enabled || Locator.GetReferenceFrame() == null)
        {
            PostAutopilotOffNotification();
            enabled = false;
            _ignoreThrustLimits = false;
            ShipEnhancements.WriteDebugMessage("nothing to orbit");
            return;
        }

        _referenceFrame = Locator.GetReferenceFrame();
        _ignoreThrustLimits = ignoreThrustLimits;
        
        var relativeVelocity = _referenceFrame.GetOWRigidBody().GetRelativeVelocity(_owRigidbody);
        var dirToReference = _referenceFrame.GetPosition() - _owRigidbody.GetWorldCenterOfMass();
        _orbitRadius = dirToReference.magnitude;
        _orbitSpeed = _referenceFrame.GetOrbitSpeed(_orbitRadius);
        _orbitalPlaneNormal = Vector3.Cross(relativeVelocity, dirToReference).normalized;
        
        enabled = true;
        
        ShipNotifications.PostOrbitAutopilotActiveNotification(_orbitRadius);
    }

    public override Vector3 ReadTranslationalInput()
    {
        if (_referenceFrame == null)
        {
            PostAutopilotOffNotification();
            enabled = false;
            ShipEnhancements.WriteDebugMessage("nothing to orbit");
            return Vector3.zero;
        }

        var currentPosition = _owRigidbody.GetWorldCenterOfMass();
        var velocityRelativeToTarget = _referenceFrame.GetOWRigidBody().GetRelativeVelocity(_owRigidbody);
        var relativeSpeed = velocityRelativeToTarget.magnitude;
        var relativePosition = currentPosition - _referenceFrame.GetPosition();
        
        var maxThrust = _ignoreThrustLimits
            ? _thrusterModel.GetMaxTranslationalThrust()
            : Mathf.Min(_rulesetDetector.GetThrustLimit(), _thrusterModel.GetMaxTranslationalThrust());
        var maxVelocityChange = maxThrust * Time.fixedDeltaTime;
        var shipThrustFactor = maxThrust / _thrusterModel.GetMaxTranslationalThrust();
        
        var targetOrbitalDirection = Vector3.ProjectOnPlane(relativePosition, _orbitalPlaneNormal).normalized;
        var targetOrbitalPosition = _referenceFrame.GetPosition() + targetOrbitalDirection*_orbitRadius;
        var targetOrbitalVelocity = Vector3.Cross(_orbitalPlaneNormal, targetOrbitalDirection) * _orbitSpeed;
        var deltaOrbitalVelocity = targetOrbitalVelocity - velocityRelativeToTarget;
        var deltaOrbitalSpeed = deltaOrbitalVelocity.magnitude;
        var deltaPositionToOrbit = targetOrbitalPosition - currentPosition;
        var distanceToOrbit = deltaPositionToOrbit.magnitude;
        var proximityThrottleFactor = Mathf.Lerp(0.2f, 1, distanceToOrbit / 100);

        if ((distanceToOrbit < 10 || distanceToOrbit / _orbitRadius < 0.05) && deltaOrbitalSpeed < 10)
        {
            ShipEnhancements.WriteDebugMessage("orbit achieved");
            return Vector3.zero;
        }
        
        var componentOfVelocityTowardsOrbit = Vector3.Dot(velocityRelativeToTarget, deltaPositionToOrbit.normalized);
        var orbitPositionImpulse = Vector3.zero;
        if (10 < distanceToOrbit)
        {
            // acceleration direction to orbit
            orbitPositionImpulse = deltaPositionToOrbit.normalized * proximityThrottleFactor;
            
            // if already traveling towards the target orbit more than a little
            if (0 < componentOfVelocityTowardsOrbit)
            {
                var decelerationForce = -maxThrust * orbitPositionImpulse * proximityThrottleFactor;
                decelerationForce += _forceDetector.GetForceAcceleration();
                var decelerationForceTowardsOrbit = Vector3.Project(decelerationForce, deltaPositionToOrbit);
                var accelerationToMatchOrbit = decelerationForceTowardsOrbit.magnitude;
                var timeToDecelerate = relativeSpeed / accelerationToMatchOrbit;
                var distanceToDecelerate = relativeSpeed * timeToDecelerate / 2;

                if (0.8 * distanceToOrbit < distanceToDecelerate)
                {
                    // flip the direction of impulse, to decelerate
                    orbitPositionImpulse *= -1;
                }
            }
        }

        var desiredImpulse = deltaOrbitalVelocity;
        if (deltaOrbitalSpeed < maxVelocityChange)
        {
            desiredImpulse *= deltaOrbitalSpeed / maxVelocityChange;
        }

        desiredImpulse += orbitPositionImpulse;
        return transform.InverseTransformDirection(shipThrustFactor * Vector3.ClampMagnitude(desiredImpulse, 1));
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
}
