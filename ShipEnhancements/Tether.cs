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
            if (Vector3.Distance(transform.TransformPoint(_anchor), _connectedRigidbody.transform.TransformPoint(_connectedAnchor)) 
                > _joint.minDistance + 5f)
            {
                _hook.DisconnectTether();
                return;
            }

            if (_connectedRigidbody == Locator.GetPlayerBody())
            {
                float tetherDist = (_connectedRigidbody.transform.TransformPoint(_connectedAnchor) - transform.TransformPoint(_anchor)).sqrMagnitude;
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
        _connectedAnchor = connectedOffset;
        _anchor = anchorOffset;

        bool attachedToPlayer = false;

        if (_connectedRigidbody != GetComponentInParent<OWRigidbody>())
        {
            _joint = _rigidbody.gameObject.AddComponent<SpringJoint>();
            _joint.connectedBody = connectedBody.GetRigidbody();

            if (connectedBody == Locator.GetPlayerBody())
            {
                attachedToPlayer = true;
                connectedBody.MoveToPosition(connectedBody.transform.position);
                _joint.massScale = 250f;
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
            _joint.minDistance = connectedBody == Locator.GetPlayerBody() ? 15f : Mathf.Min(100f, Vector3.Distance(transform.TransformPoint(_anchor), 
                _connectedRigidbody.transform.TransformPoint(_connectedAnchor)));
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

        Vector3 lineDir = _connectedRigidbody.transform.TransformPoint(_connectedAnchor) - transform.TransformPoint(_anchor);
        RaycastHit[] hits = Physics.RaycastAll(transform.TransformPoint(_anchor), lineDir, lineDir.magnitude, OWLayerMask.physicalMask);
        bool intersectingBody = false;
        foreach (RaycastHit hit in hits)
        {
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (!(rb.isKinematic || rb == GetComponentInParent<Rigidbody>() || rb == Locator.GetPlayerBody().GetRigidbody()))
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
        bool attachedToPlayer = _connectedRigidbody == Locator.GetPlayerBody();
        if (!_tetheredToSelf)
        {
            DestroyImmediate(_joint);
        }
        _tetheredToSelf = false;

        if (_collider && !ShipEnhancements.Instance.probeDestroyed && Locator.GetProbe().transform.parent == _collider.transform)
        {
            Locator.GetProbe().Unanchor();
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
        Vector3 lineDir = _connectedRigidbody.transform.TransformPoint(_connectedAnchor) - transform.TransformPoint(_anchor);
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

    public void SetAttachedRigidbody(OWRigidbody attachedBody)
    {
        _rigidbody = attachedBody;
    }

    public bool IsTethered()
    {
        return _tethered;
    }
}
