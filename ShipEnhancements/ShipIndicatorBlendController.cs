using UnityEngine;
using System.Collections.Generic;
using static ShipEnhancements.ShipEnhancements.Settings;
using System.Linq;

namespace ShipEnhancements;

public class ShipIndicatorBlendController : ColorBlendController
{
    protected override string CurrentBlend => (string)indicatorColorBlend.GetProperty();
    protected override int NumberOfOptions => int.Parse((string)indicatorColorOptions.GetProperty());
    protected override string OptionStem => "indicatorColor";

    private Material _damageScreenMat;
    private Material _masterAlarmMat;
    private Light _masterAlarmLight;
    private Light _reactorLight;
    private Material _reactorGlow;
    private DamageEffect[] _damageEffects;
    private ShipCockpitUI _cockpitUI;

    protected override void Awake()
    {
        _damageScreenMat = transform.Find("Module_Cockpit/Systems_Cockpit/ShipCockpitUI/DamageScreen/HUD_ShipDamageDisplay")
                .GetComponent<MeshRenderer>().material;
        _masterAlarmMat = transform.Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Interior/Cockpit_Interior_Chassis")
                .GetComponent<MeshRenderer>().sharedMaterials[6];
        _masterAlarmLight = transform.Find("Module_Cabin/Lights_Cabin/PointLight_HEA_MasterAlarm").GetComponent<Light>();
        _reactorLight = transform.Find("Module_Engine/Systems_Engine/ReactorComponent/ReactorDamageLight").GetComponent<Light>();
        _reactorGlow = transform.Find("Module_Engine/Systems_Engine/ReactorComponent/Structure_HEA_PlayerShip_ReactorDamageDecal")
            .GetComponent<MeshRenderer>().material;
        _cockpitUI = GetComponentInChildren<ShipCockpitUI>();
        _damageEffects = GetComponentsInChildren<DamageEffect>()
            .Where(effect => effect._damageLight != null || effect._damageLightRenderer != null).ToArray();

        Color hullColor = _damageScreenMat.GetColor("_DamagedHullFill") * 191f;
        Color compColor = _damageScreenMat.GetColor("_DamagedComponentFill") * 191f;
        
        _defaultTheme = [hullColor, 1f, compColor, 1f, _masterAlarmMat.GetColor("_Color") * 255f,
            _masterAlarmMat.GetColor("_EmissionColor") * 191f, 1f, _reactorGlow.GetColor("_EmissionColor") * 255f,
            2.6f, _reactorLight.color * 255f, _masterAlarmLight.color * 255f];

        base.Awake();
    }

    protected override void SetBlendTheme(int i, string themeName)
    {
        if (themeName == "Default")
        {
            _blendThemes[i] = _defaultTheme;
            return;
        }

        DamageTheme theme = ShipEnhancements.ThemeManager.GetDamageTheme(themeName);
        _blendThemes[i] = [theme.HullColor, theme.HullIntensity, theme.CompColor, theme.CompIntensity,
            theme.AlarmColor, theme.AlarmLitColor, theme.AlarmLitIntensity,
            theme.ReactorColor, theme.ReactorIntensity, theme.ReactorLight, theme.IndicatorLight];
    }

    protected override void UpdateLerp(List<object> start, List<object> end, float lerp)
    {
        SetColor(GetLerp(start, end, lerp));
    }

    protected override List<object> GetLerp(List<object> start, List<object> end, float lerp)
    {
        var newHull = Color.Lerp((Color)start[0], (Color)end[0], lerp);
        var newHullIntensity = Mathf.Lerp((float)start[1], (float)end[1], lerp);
        var newComp = Color.Lerp((Color)start[2], (Color)end[2], lerp);
        var newCompIntensity = Mathf.Lerp((float)start[3], (float)end[3], lerp);
        var newAlarm = Color.Lerp((Color)start[4], (Color)end[4], lerp);
        var newAlarmLit = Color.Lerp((Color)start[5], (Color)end[5], lerp);
        var newAlarmIntensity = Mathf.Lerp((float)start[6], (float)end[6], lerp);
        var newReactor = Color.Lerp((Color)start[7], (Color)end[7], lerp);
        var newReactorIntensity = Mathf.Lerp((float)start[8], (float)end[8], lerp);
        var newReactorLight = Color.Lerp((Color)start[9], (Color)end[9], lerp);
        var newLight = Color.Lerp((Color)start[10], (Color)end[10], lerp);

        return [newHull, newHullIntensity, newComp, newCompIntensity,
            newAlarm, newAlarmLit, newAlarmIntensity, newReactor,
            newReactorIntensity, newReactorLight, newLight];
    }

    protected override void UpdateRainbowTheme(int index, Color color)
    {
        _blendThemes[index] = [color, 1f, color, 1f, color, color, 1f, color, 1.5f, color, color];
    }

    protected override void SetColor(Color color)
    {
        SetColor([color, 1f, color, 1f, color, color, 1f, color, 1.5f, color, color]);
    }

    protected override void SetColor(List<object> theme)
    {
        _damageScreenMat.SetColor("_DamagedHullFill", (Color)theme[0] / 191f * Mathf.Pow(2, (float)theme[1]));
        _damageScreenMat.SetColor("_DamagedComponentFill", (Color)theme[2] / 191f * Mathf.Pow(2, (float)theme[3]));
        _masterAlarmMat.SetColor("_Color", (Color)theme[4] / 255f);
        _cockpitUI._damageLightColor = (Color)theme[5] / 191f * Mathf.Pow(2, (float)theme[6]);
        _masterAlarmMat.SetColor("_EmissionColor", _cockpitUI._damageLightColor * (_cockpitUI._damageLightOn ? 1f : 0f));
        _masterAlarmLight.color = (Color)theme[10] / 255f;
        Color reactorColor = (Color)theme[7] / 191f;
        reactorColor.a = 1f;
        _reactorGlow.SetColor("_EmissionColor", reactorColor * (float)theme[8]);
        _reactorLight.color = (Color)theme[9] / 255f;
        foreach (DamageEffect effect in _damageEffects)
        {
            if (effect._damageLight)
            {
                effect._damageLight._light.color = (Color)theme[10] / 255f;
            }
            if (effect._damageLightRenderer)
            {
                //effect._damageLightRenderer.SetColor(theme[4]);
                effect._damageLightRendererColor = (Color)theme[5] / 191f * Mathf.Pow(2, (float)theme[6]);
            }
        }
    }

    protected override void ResetColor()
    {
        SetColor(_defaultTheme);
        base.ResetColor();
    }
}
