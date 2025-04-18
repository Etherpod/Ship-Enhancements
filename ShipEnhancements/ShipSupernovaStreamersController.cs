using System;
using UnityEngine;

namespace ShipEnhancements;

[RequireComponent(typeof(VectionFieldEmitter))]
public class ShipSupernovaStreamersController : MonoBehaviour
{
    [SerializeField]
    private float _playDist = 10000f;

    private VectionFieldEmitter _vectionFieldEmitter;
    private SupernovaEffectController _supernova;

    private void Awake()
    {
        this._vectionFieldEmitter = base.GetComponent<VectionFieldEmitter>();
        GlobalMessenger.AddListener("FlashbackStart", new Callback(this.OnFlashbackStart));
        this._vectionFieldEmitter.enabled = false;
        base.enabled = false;
    }

    private void OnDestroy()
    {
        GlobalMessenger.RemoveListener("FlashbackStart", new Callback(this.OnFlashbackStart));
    }

    public void OnSupernovaStart(SupernovaEffectController controller)
    {
        _supernova = controller;
        base.enabled = true;
    }

    private void OnFlashbackStart()
    {
        base.enabled = false;
    }

    private void SetParentHelper(Transform parent)
    {
        if (base.transform.parent != parent)
        {
            base.transform.SetParent(parent);
            base.transform.localPosition = Vector3.zero;
            base.transform.localRotation = Quaternion.identity;
        }
    }

    private void LateUpdate()
    {
        OWCamera activeCamera = Locator.GetActiveCamera();
        Vector3 vector = _supernova.transform.position - activeCamera.transform.position;
        float magnitude = vector.magnitude;
        vector /= magnitude;
        OWRigidbody attachedOWRigidbody = activeCamera.GetAttachedOWRigidbody(false);
        if (attachedOWRigidbody != null)
        {
            SectorDetector componentInChildren = attachedOWRigidbody.GetComponentInChildren<SectorDetector>();
            if (componentInChildren != null)
            {
                Sector sector = componentInChildren.GetLastEnteredSector();
                if (sector != null && sector.GetName() == Sector.Name.Ship)
                {
                    sector = Locator.GetShipDetector().GetComponent<SectorDetector>().GetLastEnteredSector();
                }
                if (sector != null)
                {
                    this.SetParentHelper(sector.GetOWRigidbody().transform);
                }
                else
                {
                    this.SetParentHelper(this._supernova.transform);
                }
            }
            else
            {
                this.SetParentHelper(attachedOWRigidbody.transform);
            }
        }
        else
        {
            this.SetParentHelper(this._supernova.transform);
        }
        float supernovaRadius = this._supernova.GetSupernovaRadius();
        bool flag = Mathf.Abs(magnitude - supernovaRadius) < this._playDist;
        this._vectionFieldEmitter.emitterTransform = activeCamera.transform;
        this._vectionFieldEmitter.directionalDir = base.transform.InverseTransformDirection(-vector);
        this._vectionFieldEmitter.enabled = flag;
    }
}
