using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ShipEnhancements;

public class ExpeditionFlagItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.ExpeditionFlagType;

    [SerializeField] private GameObject _itemModelParent;
    [SerializeField] private GameObject _objModelParent;
    [SerializeField] private Collider _flagCollider;
    [SerializeField] private OWTriggerVolume _colTrigger;
    [SerializeField] private MeshRenderer _decalRenderer;

    private readonly Vector3 _holdOffset = new(0f, -0.4f, 0f);
    private Vector3 _defaultOffset;

    public override string GetDisplayName()
    {
        return "Expedition Flag";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _defaultOffset = _itemModelParent.transform.localPosition;
        _colTrigger.OnExit += OnExit;
    }

    private void Start()
    {
        Texture2D tex = null;
        byte[] fileData;

        List<string> files = [];
        files.AddRange(Directory.GetFiles(Path.Combine(ShipEnhancements.Instance.ModHelper.Manifest.ModFolderPath, "ExpeditionFlagIcons"), 
            "*.jpg", SearchOption.AllDirectories));
        files.AddRange(Directory.GetFiles(Path.Combine(ShipEnhancements.Instance.ModHelper.Manifest.ModFolderPath, "ExpeditionFlagIcons"),
            "*.png", SearchOption.AllDirectories));
        if (files.Count > 0)
        {
            fileData = File.ReadAllBytes(files[Random.Range(0, files.Count)]);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }

        if (tex != null)
        {
            _decalRenderer.material.SetTexture("_MainTex", tex);
        }

        SetIsDropped(false);
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);

        SetIsDropped(false);
        _itemModelParent.transform.localPosition = _defaultOffset + _holdOffset;
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);

        SetIsDropped(true);

        _flagCollider.enabled = false;
        ShipEnhancements.Instance.ModHelper.Events.Unity.FireInNUpdates(() =>
        {
            if (_colTrigger.getTrackedObjects()
            .Where(obj => !obj.GetAttachedOWRigidbody()?.IsKinematic() ?? false)
            .ToArray().Length == 0)
            {
                _flagCollider.enabled = true;
            }
        }, 5);
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);

        _itemModelParent.transform.localPosition = _defaultOffset;
    }

    public void SetIsDropped(bool dropped)
    {
        _itemModelParent.SetActive(!dropped);
        _objModelParent.SetActive(dropped);
    }

    private void OnExit(GameObject hitObj)
    {
        if (!_flagCollider.enabled
            && _colTrigger.getTrackedObjects()
            .Where(obj => !obj.GetAttachedOWRigidbody()?.IsKinematic() ?? false)
            .ToArray().Length == 0)
        {
            _flagCollider.enabled = true;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _colTrigger.OnExit -= OnExit;
    }
}
