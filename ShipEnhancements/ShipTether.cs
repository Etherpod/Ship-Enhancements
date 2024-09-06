using UnityEngine;
using UnityEngine.InputSystem;

namespace ShipEnhancements;

public class ShipTether : MonoBehaviour
{
    private bool _tethered = false;
    private SpringJoint _joint;
    //private LineRenderer _line;
    private Transform _tetherMesh;
    private GameObject _tether;
    private OWRigidbody _rigidbody;
    private OWRigidbody _connectedRigidbody;
    private Vector3 _connectedAnchor;
    private TetherHookItem _hook;
    private TetherHookItem _connectedHook;
    private bool _tetheredToSelf = false;
    private CapsuleCollider _collider;

    private void Awake()
    {
        _hook = GetComponent<TetherHookItem>();
    }

    private void Update()
    {
        if (_joint || _tetheredToSelf)
        {
            UpdateTetherLine();
        }

        if (_joint)
        {
            if (Vector3.Distance(transform.position, _connectedRigidbody.transform.TransformPoint(_connectedAnchor)) > _joint.minDistance + 5f)
            {
                _hook.DisconnectTether();
                return;
            }

            if (Keyboard.current.rightBracketKey.isPressed)
            {
                _joint.minDistance += Time.deltaTime * 2f;
            }
            else if (Keyboard.current.leftBracketKey.isPressed)
            {
                _joint.minDistance -= Time.deltaTime * 2f;
            }

            if (_connectedRigidbody == Locator.GetPlayerBody())
            {
                if (Vector3.Distance(_connectedRigidbody.transform.TransformPoint(_connectedAnchor), Vector3.zero) > _joint.minDistance - 1f)
                {
                    _joint.damper = 0.01f;
                }
                else
                {
                    _joint.damper = 0f;
                }
            }
        }
    }

    public void CreateTether(OWRigidbody connectedBody, Vector3 offset)
    {
        if (_tethered) return;

        _tethered = true;

        _connectedRigidbody = connectedBody;
        _connectedAnchor = offset;

        if (_connectedRigidbody != GetComponentInParent<OWRigidbody>())
        {
            _joint = _rigidbody.gameObject.AddComponent<SpringJoint>();
            _joint.connectedBody = connectedBody.GetRigidbody();

            if (connectedBody == Locator.GetPlayerBody())
            {
                connectedBody.MoveToPosition(connectedBody.transform.position);
            }

            _joint.anchor = transform.localPosition;
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _connectedAnchor;
            _joint.enableCollision = true;
            _joint.maxDistance = 0f;
            _joint.minDistance = 15f;
            _joint.spring = 0.2f;
            _joint.damper = 0f;
        }
        else
        {
            _tetheredToSelf = true;
        }

        _tetherMesh = Instantiate(ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/TetherLine.prefab"), transform).transform;
        AssetBundleUtilities.ReplaceShaders(_tetherMesh.gameObject);
        _tetherMesh.localPosition = Vector3.zero;
        if (_tetheredToSelf)
        {
            _collider = _tetherMesh.GetComponentInChildren<CapsuleCollider>();
            _collider.enabled = true;
        }
        UpdateTetherLine();
    }

    public void DisconnectTether()
    {
        if (!_tethered) return;

        _tethered = false;
        if (!_tetheredToSelf)
        {
            DestroyImmediate(_joint);
        }
        _tetheredToSelf = false;
        Destroy(_tetherMesh.gameObject);
        if (_connectedHook)
        {
            _connectedHook.DisconnectFromHook();
            _connectedHook = null;
        }
    }

    public void TransferTether(OWRigidbody newBody, Vector3 offset, TetherHookItem hook)
    {
        if (!_tethered) return;

        DisconnectTether();
        CreateTether(newBody, offset);
        _connectedHook = hook;
    }

    private void UpdateTetherLine()
    {
        Vector3 lineDir = _connectedRigidbody.transform.TransformPoint(_connectedAnchor) - transform.position;
        float magnitude = lineDir.magnitude;
        Transform scaleParent = _tetherMesh.Find("ScaleParent");
        _tetherMesh.rotation = Quaternion.LookRotation(lineDir);
        scaleParent.localScale = new Vector3(scaleParent.localScale.x, scaleParent.localScale.y, magnitude);
        scaleParent.GetComponentInChildren<MeshRenderer>().material.SetTextureScale("_MainTex", new Vector2(1f, magnitude));
        if (_tetheredToSelf)
        {
            _collider.center = new Vector3(0f, 0f, 0.5f * magnitude);
            _collider.height = magnitude;
        }
    }

    public TetherHookItem GetHook()
    {
        return _hook;
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
