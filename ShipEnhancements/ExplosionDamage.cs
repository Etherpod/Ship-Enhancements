using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ExplosionDamage : MonoBehaviour
{
    public bool damageShip
    {
        get
        {
            return _damageShip;
        }
        set
        {
            _damageShip = value;
        }
    }

    public bool damageFragment
    {
        get
        {
            return _damageFragment;
        }
        set
        {
            _damageFragment = value;
        }
    }

    public bool unparent
    {
        get
        {
            return _unparent;
        }
        set
        {
            _unparent = value;
        }
    }

    private bool _damageShip;
    private bool _damageFragment;
    private bool _unparent;

    private List<ShipHull> _trackedHulls = [];
    private SphereCollider _collider;
    private ExplosionController _explosion;

    private void Awake()
    {
        _collider = GetComponent<SphereCollider>();
        _explosion = GetComponentInParent<ExplosionController>();
    }

    private void Start()
    {
        _collider.enabled = false;
        enabled = false;
    }

    public void OnExplode()
    {
        _collider.enabled = true;
        gameObject.layer = 0;
        if (_unparent)
        {
            transform.parent = null;
        }
        enabled = true;
    }

    private void Update()
    {
        if (_explosion == null)
        {
            enabled = false;
            _collider.enabled = false;
            return;
        }
        if (_unparent)
        {
            transform.position = _explosion.transform.position;
        }
        float lerp = Mathf.Clamp01(_explosion._timer / _explosion._length);
        _collider.radius = Mathf.Lerp(0.1f, 1f, lerp * 2f);
    }

    private void OnTriggerEnter(Collider hitObj)
    {
        if (ShipEnhancements.InMultiplayer && !ShipEnhancements.QSBAPI.GetIsHost())
        {
            return;
        }

        if (_damageShip && (float)shipExplosionMultiplier.GetProperty() > 0 && (float)shipDamageMultiplier > 0)
        {
            ShipHull hull = hitObj.GetComponentInParent<ShipHull>();
            if (hull != null && !_trackedHulls.Contains(hull))
            {
                _trackedHulls.Add(hull);
                bool newlyDamaged = false;

                float num = UnityEngine.Random.Range(0.9f, 1.1f) * Mathf.Sqrt(1f/20f * (float)shipExplosionMultiplier.GetProperty())
                    * (float)shipDamageMultiplier.GetProperty();
                hull._integrity = Mathf.Max(hull._integrity - num, 0f);
                for (int i = 0; i < hull._components.Length; i++)
                {
                    if (UnityEngine.Random.value < num)
                    {
                        hull._components[i].SetDamaged(true);
                    }
                }
                if (!hull._damaged)
                {
                    hull._damaged = true;
                    newlyDamaged = true;

                    var eventDelegate = (MulticastDelegate)typeof(ShipHull).GetField("OnDamaged", BindingFlags.Instance
                        | BindingFlags.NonPublic | BindingFlags.Public).GetValue(hull);
                    if (eventDelegate != null)
                    {
                        foreach (var handler in eventDelegate.GetInvocationList())
                        {
                            handler.Method.Invoke(handler.Target, [hull]);
                        }
                    }
                }
                if (hull._damageEffect != null)
                {
                    hull._damageEffect.SetEffectBlend(1f - hull._integrity);
                }

                if (ShipEnhancements.InMultiplayer)
                {
                    ShipEnhancements.QSBInteraction.SetHullDamaged(hull, newlyDamaged);
                }

                if (hull.shipModule is ShipDetachableModule)
                {
                    ShipDetachableModule module = hull.shipModule as ShipDetachableModule;
                    if (hull.integrity <= 0f && !module.isDetached)
                    {
                        module.Detach();
                    }
                }
                else if (hull.shipModule is ShipLandingModule)
                {
                    ShipLandingModule module = hull.shipModule as ShipLandingModule;
                    if (hull.integrity <= 0f)
                    {
                        module.DetachAllLegs();
                    }
                }
            }
        }
        if (_damageFragment)
        {
            //ShipEnhancements.WriteDebugMessage(hitObj.gameObject.name);
            FragmentIntegrity fragment = hitObj.GetComponentInParent<FragmentIntegrity>();
            if (fragment != null)
            {
                if ((float)shipExplosionMultiplier.GetProperty() >= 1f)
                {
                    fragment.AddDamage(UnityEngine.Random.Range(300, 500));
                }
                else
                {
                    float lerp = Mathf.InverseLerp(0f, 1f, (float)shipExplosionMultiplier.GetProperty());
                    fragment.AddDamage(Mathf.Lerp(0f, 100f, lerp));
                }
            }
        }

        FuelTankItem tank = hitObj.GetComponentInParent<FuelTankItem>();
        if (tank != null)
        {
            tank.Explode();
        }

        AnglerfishController angler = hitObj.GetComponentInParent<AnglerfishController>();
        if (angler != null)
        {
            angler.ChangeState(AnglerfishController.AnglerState.Stunned);
            angler.GetComponentInChildren<AnglerfishFluidVolume>().SetVolumeActivation(false);
        }
    }
}
