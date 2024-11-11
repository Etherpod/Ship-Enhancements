using System.Linq;
using UnityEngine;

namespace ShipEnhancements;

public class RainbowShipDamage : MonoBehaviour
{
    private Material _damageScreenMat;
    private Material _masterAlarmMat;
    private Light _masterAlarmLight;
    private DamageEffect[] _damageEffects;
    private ShipCockpitUI _cockpitUI;
    private float _colorTransitionTime = 6f;
    private float _red;
    private float _green;
    private float _blue;
    private int _index;
    private float _lastDelta;

    private void Start()
    {
        _red = 1f;
        _green = 0f;
        _blue = 0f;

        _damageScreenMat = transform.Find("Module_Cockpit/Systems_Cockpit/ShipCockpitUI/DamageScreen/HUD_ShipDamageDisplay")
                .GetComponent<MeshRenderer>().material;
        _masterAlarmMat = transform.Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Interior/Cockpit_Interior_Chassis")
                .GetComponent<MeshRenderer>().sharedMaterials[6];
        _masterAlarmLight = transform.Find("Module_Cabin/Lights_Cabin/PointLight_HEA_MasterAlarm").GetComponent<Light>();
        _cockpitUI = GetComponentInChildren<ShipCockpitUI>();
        _damageEffects = GetComponentsInChildren<DamageEffect>()
            .Where(effect => effect._damageLight != null || effect._damageLightRenderer != null).ToArray();
    }

    private void FixedUpdate()
    {
        float num = Mathf.InverseLerp(0f, _colorTransitionTime, Time.time % (_colorTransitionTime));

        if (_lastDelta > num)
        {
            _index++;
            if (_index > 5) _index = 0;
        }

        if (_index == 0)
        {
            _green = Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 1)
        {
            _red = 1 - Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 2)
        {
            _blue = Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 3)
        {
            _green = 1 - num;
        }
        else if (_index == 4)
        {
            _red = Mathf.Lerp(0f, 1f, num);
        }
        else if (_index == 5)
        {
            _blue = 1 - Mathf.Lerp(0f, 1f, num);
        }
        Color color = new Color(_red, _green, _blue);

        _damageScreenMat.SetColor("_DamagedHullFill", color);
        _damageScreenMat.SetColor("_DamagedComponentFill", color);
        _masterAlarmMat.SetColor("_Color", color * 0.6f);
        _cockpitUI._damageLightColor = color * 1.1f;
        _masterAlarmLight.color = color;
        foreach (DamageEffect effect in _damageEffects)
        {
            if (effect._damageLight)
            {
                effect._damageLight._light.color = color;
            }
            if (effect._damageLightRenderer)
            {
                effect._damageLightRenderer.SetColor(color * 0.6f);
                effect._damageLightRendererColor = color * 1.3f;
            }
        }

        _lastDelta = num;
    }
}
