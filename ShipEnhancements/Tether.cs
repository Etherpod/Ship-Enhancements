using UnityEngine;

namespace ShipEnhancements;

public class Tether : MonoBehaviour
{
    private bool _tethered = false;
    private SpringJoint _joint;
    private Transform _tetherMesh;
    private GameObject _tether;
    private OWRigidbody _rigidbody;
    private OWRigidbody _connectedRigidbody;
    private Vector3 _connectedAnchor;
    private Vector3 _anchor;
    private TetherHookItem _hook;
    private TetherHookItem _connectedHook;
    private bool _tetheredToSelf = false;
    private CapsuleCollider _collider;
    private readonly float _minTetherDistance = 0.25f;
    private readonly float _maxTetherDistance = 50f;

    private Transform _connectedTransform;
    private bool _remoteTether;
    private bool _addedJointObj;

    private void Awake()
    {
        _hook = GetComponent<TetherHookItem>();
    }

    private void Update()
    {
        if (_joint || _remoteTether || _tetheredToSelf)
        {
            UpdateTetherLine();
        }

        if (_joint && !_remoteTether)
        {
            if (Vector3.Distance(transform.TransformPoint(_anchor), _connectedTransform.TransformPoint(_connectedAnchor)) 
                > _joint.minDistance + 5f)
            {
                _hook.DisconnectTether();
                return;
            }

            if (_connectedRigidbody == SELocator.GetPlayerBody())
            {
                float tetherDist = (_connectedTransform.TransformPoint(_connectedAnchor) - transform.TransformPoint(_anchor)).sqrMagnitude;
                if (OWInput.IsPressed(InputLibrary.toolOptionDown) && _joint.minDistance < _maxTetherDistance)
                {
                    _joint.minDistance += Time.deltaTime * 5f;
                    // Play reel noise
                }
                else if (tetherDist < Mathf.Pow(_joint.minDistance + 2f, 2) && OWInput.IsPressed(InputLibrary.toolOptionUp) && _joint.minDistance > _minTetherDistance)
                {
                    _joint.minDistance -= Time.deltaTime * 5f;
                    // Play reel noise
                }

                if (OWInput.IsPressed(InputLibrary.toolActionSecondary, 0.8f))
                {
                    _hook.DisconnectTether();
                    // Play untether noise
                }
            }
        }
    }

    public void CreateTether(OWRigidbody connectedBody, Vector3 anchorOffset, Vector3 connectedOffset)
    {
        if (_tethered) return;

        _tethered = true;

        _connectedRigidbody = connectedBody;
        _connectedTransform = connectedBody.transform;
        _connectedAnchor = connectedOffset;
        _anchor = anchorOffset;

        bool attachedToPlayer = false;

        if (_connectedRigidbody != GetComponentInParent<OWRigidbody>())
        {
            _joint = _rigidbody.gameObject.AddComponent<SpringJoint>();
            _joint.connectedBody = connectedBody.GetRigidbody();

            if (connectedBody == SELocator.GetPlayerBody())
            {
                attachedToPlayer = true;
                connectedBody.MoveToPosition(connectedBody.transform.position);
                _joint.massScale = 250f;

                if (ShipEnhancements.InMultiplayer && !ShipEnhancements.QSBAPI.GetIsHost() && !_rigidbody.IsKinematic())
                {
                    _joint.connectedMassScale = 1000f;
                    _joint.massScale = 0.001f;
                }
            }
            else
            {
                _joint.connectedMassScale = 0.001f;
                _joint.massScale = 1000f;
            }

            _joint.anchor = transform.localPosition + anchorOffset;
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _connectedAnchor;
            _joint.enableCollision = true;
            _joint.maxDistance = 0f;
            _joint.minDistance = connectedBody == SELocator.GetPlayerBody() ? 15f : Mathf.Min(100f, Vector3.Distance(transform.TransformPoint(_anchor), 
                _connectedTransform.TransformPoint(_connectedAnchor)));
            _joint.spring = 0.2f;
            _joint.damper = 0f;
        }
        else
        {
            _tetheredToSelf = true;
        }

        _tetherMesh = Instantiate(ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/TetherLine.prefab"), transform).transform;
        AssetBundleUtilities.ReplaceShaders(_tetherMesh.gameObject);
        _tetherMesh.localPosition = _anchor;

        Vector3 lineDir = _connectedTransform.TransformPoint(_connectedAnchor) - transform.TransformPoint(_anchor);
        RaycastHit[] hits = Physics.RaycastAll(transform.TransformPoint(_anchor), lineDir, lineDir.magnitude, OWLayerMask.physicalMask);
        bool intersectingBody = false;
        foreach (RaycastHit hit in hits)
        {
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (!(rb.isKinematic || rb == GetComponentInParent<Rigidbody>() || rb == SELocator.GetPlayerBody().GetRigidbody()))
            {
                intersectingBody = true;
                break;
            }
        }
        if (_tetheredToSelf && !intersectingBody)
        {
            _collider = _tetherMesh.GetComponentInChildren<CapsuleCollider>();
            _collider.enabled = true;
        }

        UpdateTetherLine();

        if (attachedToPlayer)
        {
            GlobalMessenger.FireEvent("AttachPlayerTether");
        }
    }

    public void DisconnectTether()
    {
        if (!_tethered) return;

        _tethered = false;
        _remoteTether = false;
        bool attachedToPlayer = _connectedRigidbody == SELocator.GetPlayerBody();
        if (_addedJointObj && _joint)
        {
            DestroyImmediate(_joint.gameObject);
        }
        else if (!_tetheredToSelf && _joint)
        {
            DestroyImmediate(_joint);
        }
        _addedJointObj = false;
        _tetheredToSelf = false;

        if (_collider && !ShipEnhancements.Instance.probeDestroyed && SELocator.GetProbe().transform.parent == _collider.transform)
        {
            SELocator.GetProbe().Unanchor();
        }
        Destroy(_tetherMesh.gameObject);

        _hook.DisconnectFromHook();
        if (_connectedHook)
        {
            _connectedHook.DisconnectFromHook();
            _connectedHook = null;
        }

        if (attachedToPlayer)
        {
            GlobalMessenger.FireEvent("DetachPlayerTether");
        }
    }

    public void TransferTether(OWRigidbody newBody, Vector3 offset, TetherHookItem hook)
    {
        if (!_tethered) return;

        DisconnectTether();
        CreateTether(newBody, _anchor, offset);
        _connectedHook = hook;
        _hook.TransferToHook();
        hook.TransferToHook();
    }

    private void UpdateTetherLine()
    {
        Vector3 lineDir = _connectedTransform.TransformPoint(_connectedAnchor) - transform.TransformPoint(_anchor);
        float magnitude = lineDir.magnitude;
        Transform scaleParent = _tetherMesh.Find("ScaleParent");
        _tetherMesh.rotation = Quaternion.LookRotation(lineDir);
        scaleParent.localScale = new Vector3(scaleParent.localScale.x, scaleParent.localScale.y, magnitude);
        scaleParent.GetComponentInChildren<MeshRenderer>().material.SetTextureScale("_MainTex", new Vector2(1f, magnitude));
        if (_tetheredToSelf && _collider)
        {
            _collider.center = new Vector3(0f, 0f, 0.5f * magnitude);
            _collider.height = magnitude;
        }
    }

    public void CreateRemoteTether(Transform connectedObj, Vector3 anchorOffset, Vector3 connectedOffset)
    {
        if (_tethered) return;

        _tethered = true;
        _remoteTether = true;

        _anchor = anchorOffset;
        _connectedTransform = connectedObj;
        _connectedAnchor = connectedOffset;

        _tetherMesh = Instantiate(ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/TetherLine.prefab"), transform).transform;
        AssetBundleUtilities.ReplaceShaders(_tetherMesh.gameObject);
        _tetherMesh.localPosition = _anchor;

        // old code
        /*if (false && !(connectedObj.IsChildOf(transform) || transform.IsChildOf(connectedObj)) && (!GetComponentInParent<OWRigidbody>()?.IsKinematic() ?? false))
        {
            _connectedRigidbody = GetComponentInParent<OWRigidbody>();
            _connectedTransform = transform;
            _anchorTransform = connectedObj;
            _connectedAnchor = anchorOffset;

            GameObject jointObj = new("REMOTE_TetherJoint");
            jointObj.transform.parent = connectedObj;
            jointObj.transform.localPosition = Vector3.zero;
            OWRigidbody rb = jointObj.AddComponent<OWRigidbody>();
            rb.MakeKinematic();
            _addedJointObj = true;

            _joint = jointObj.AddComponent<SpringJoint>();
            _joint.connectedBody = _connectedRigidbody.GetRigidbody();
            _joint.connectedMassScale = 0.001f;
            _joint.massScale = 1000f;
            _joint.anchor = connectedOffset;
            ShipEnhancements.WriteDebugMessage(_joint.anchor);
            _anchor = _joint.anchor;
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _connectedAnchor;
            _joint.enableCollision = true;
            _joint.maxDistance = 0f;
            _joint.minDistance = Mathf.Min(100f, Vector3.Distance(transform.TransformPoint(_anchor),
                _connectedTransform.TransformPoint(_connectedAnchor)));
            _joint.spring = 0.2f;
            _joint.damper = 0f;

            _tetherMesh = Instantiate(ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/TetherLine.prefab"), connectedObj).transform;
            AssetBundleUtilities.ReplaceShaders(_tetherMesh.gameObject);
            _tetherMesh.localPosition = _anchor;
        }
        else
        {
            _anchor = anchorOffset;
            _connectedTransform = connectedObj;
            _connectedAnchor = connectedOffset;

            _tetherMesh = Instantiate(ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/TetherLine.prefab"), transform).transform;
            AssetBundleUtilities.ReplaceShaders(_tetherMesh.gameObject);
            _tetherMesh.localPosition = _anchor;
        }*/

        _tetheredToSelf = _connectedTransform == transform;

        Vector3 lineDir = _connectedTransform.TransformPoint(_connectedAnchor) - transform.TransformPoint(_anchor);
        RaycastHit[] hits = Physics.RaycastAll(transform.TransformPoint(_anchor), lineDir, lineDir.magnitude, OWLayerMask.physicalMask);
        bool intersectingBody = false;
        foreach (RaycastHit hit in hits)
        {
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (!(rb.isKinematic || rb == GetComponentInParent<Rigidbody>() || rb == SELocator.GetPlayerBody().GetRigidbody()))
            {
                intersectingBody = true;
                break;
            }
        }
        if (_tetheredToSelf && !intersectingBody)
        {
            _collider = _tetherMesh.GetComponentInChildren<CapsuleCollider>();
            _collider.enabled = true;
        }

        UpdateTetherLine();
    }

    public void SetAttachedRigidbody(OWRigidbody attachedBody)
    {
        _rigidbody = attachedBody;
    }

    public bool IsTethered()
    {
        return _tethered;
    }

    public TetherHookItem GetHook()
    {
        return _hook;
    }
}
