using UnityEngine;
using UnityEngine.InputSystem;

namespace ShipEnhancements;

public class ShipTether : MonoBehaviour
{
    private OWRigidbody _rigibody;
    private bool _tethered = false;
    private SpringJoint _joint;
    private LineRenderer _line;
    private GameObject _tether;

    private void Start()
    {
        _rigibody = GetComponent<OWRigidbody>();
    }

    private void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            if (!_tethered)
            {
                _tethered = true;
                SpawnTether();
            }
            else if (_joint)
            {
                _tethered = false;
                DestroyImmediate(_joint);
            }
        }
        if (_joint)
        {
            if (Keyboard.current.rightBracketKey.wasPressedThisFrame)
            {

            }
        }
    }

    private void SpawnTether()
    {
        _joint = Locator.GetShipTransform().gameObject.AddComponent<SpringJoint>();
        _joint.connectedBody = _rigibody.GetRigidbody();

        _rigibody.MoveToPosition(_rigibody.transform.position);

        _joint.anchor = Vector3.zero;
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedAnchor = Vector3.zero;
        _joint.enableCollision = true;
        _joint.maxDistance = 0f;
        _joint.minDistance = 20f;
        _joint.spring = 0.2f;
        _joint.damper = 0;
    }
}
