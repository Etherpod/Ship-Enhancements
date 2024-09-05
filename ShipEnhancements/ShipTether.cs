using UnityEngine;
using UnityEngine.InputSystem;

namespace ShipEnhancements;

public class ShipTether : MonoBehaviour
{
    private bool _tethered = false;
    private SpringJoint _joint;
    private LineRenderer _line;
    private GameObject _tether;
    private OWRigidbody _rigidbody;
    private OWRigidbody _connectedRigidbody;

    private void Update()
    {
        if (_joint)
        {
            _line.SetPositions([transform.position, _connectedRigidbody.transform.position]);

            if (Keyboard.current.rightBracketKey.isPressed)
            {
                _joint.minDistance += Time.deltaTime * 2f;
            }
            else if (Keyboard.current.leftBracketKey.isPressed)
            {
                _joint.minDistance -= Time.deltaTime * 2f;
            }

            if (Vector3.Distance(Locator.GetShipTransform().position, Vector3.zero) > _joint.minDistance - 1f)
            {
                _joint.damper = 0.01f;
            }
            else
            {
                _joint.damper = 0f;
            }
        }
    }

    public void CreateTether(OWRigidbody connectedBody)
    {
        if (_tethered) return;

        _tethered = true;

        _connectedRigidbody = connectedBody;

        _joint = _rigidbody.gameObject.AddComponent<SpringJoint>();
        _joint.connectedBody = connectedBody.GetRigidbody();

        if (connectedBody == Locator.GetPlayerBody())
        {
            connectedBody.MoveToPosition(connectedBody.transform.position);
        }

        _joint.anchor = transform.localPosition;
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedAnchor = Vector3.zero;
        _joint.enableCollision = true;
        _joint.maxDistance = 0f;
        _joint.minDistance = 15f;
        _joint.spring = 0.2f;
        _joint.damper = 0f;

        _line = Instantiate(ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/TetherLine.prefab"), transform).GetComponent<LineRenderer>();
        AssetBundleUtilities.ReplaceShaders(_line.gameObject);
        _line.transform.localPosition = Vector3.zero;
        _line.SetPositions([transform.position, _connectedRigidbody.transform.position]);
    }

    public void DisconnectTether()
    {
        if (!_tethered) return;

        _tethered = false;
        DestroyImmediate(_joint);
        Destroy(_line.gameObject);
    }

    public void SetAttachedRigidbody(OWRigidbody attachedBody)
    {
        _rigidbody = attachedBody;
    }

    public bool IsTethered()
    {
        return _tethered;
    }
}
