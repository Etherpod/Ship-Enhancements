using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ShipEnhancements;

public class ExplosionDamage : MonoBehaviour
{
    [SerializeField]
    public bool _damageShip;
    [SerializeField]
    public bool _damageFragment;

    private List<ShipHull> _trackedHulls = [];
    private Collider _collider;
    private Transform _initialParent;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    private void Start()
    {
        _collider.enabled = false;
        _initialParent = transform.parent;
        enabled = false;
    }

    public void OnExplode()
    {
        _collider.enabled = true;
        //transform.parent = null;
        gameObject.layer = 0;
        //enabled = true;
    }

    private void FixedUpdate()
    {
        if (_initialParent == null)
        {
            enabled = false;
            return;
        }
        transform.position = _initialParent.transform.position;
    }

    private void OnTriggerEnter(Collider hitObj)
    {
        if (_damageShip)
        {
            ShipHull hull = hitObj.GetComponentInParent<ShipHull>();
            if (hull != null && !_trackedHulls.Contains(hull))
            {
                float num = UnityEngine.Random.Range(0.4f, 0.7f);
                hull._integrity = Mathf.Max(hull._integrity - num, 0f);
                if (!hull._damaged)
                {
                    hull._damaged = true;

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
                _trackedHulls.Add(hull);
            }
        }
        if (_damageFragment)
        {
            ShipEnhancements.WriteDebugMessage(hitObj.gameObject.name);
            FragmentIntegrity fragment = hitObj.GetComponentInParent<FragmentIntegrity>();
            if (fragment != null)
            {
                fragment.AddDamage(1000f);
            }
        }
    }

    /*private void OnDestroy()
    {
        _triggerVolume.OnEntry -= OnEntry;
    }*/
}
