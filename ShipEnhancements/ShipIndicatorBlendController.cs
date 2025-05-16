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
    private DamageEffect[] _damageEffects;
    private ShipCockpitUI _cockpitUI;

    protected override void Awake()
    {
        _damageScreenMat = transform.Find("Module_Cockpit/Systems_Cockpit/ShipCockpitUI/DamageScreen/HUD_ShipDamageDisplay")
                .GetComponent<MeshRenderer>().material;
        _masterAlarmMat = transform.Find("Module_Cockpit/Geo_Cockpit/Cockpit_Geometry/Cockpit_Interior/Cockpit_Interior_Chassis")
                .GetComponent<MeshRenderer>().sharedMaterials[6];
        _masterAlarmLight = transform.Find("Module_Cabin/Lights_Cabin/PointLight_HEA_MasterAlarm").GetComponent<Light>();
        _cockpitUI = GetComponentInChildren<ShipCockpitUI>();
        _damageEffects = GetComponentsInChildren<DamageEffect>()
            .Where(effect => effect._damageLight != null || effect._damageLightRenderer != null).ToArray();

        Color hullColor = _damageScreenMat.GetColor("_DamagedHullFill") * 191f;
        Color compColor = _damageScreenMat.GetColor("_DamagedComponentFill") * 191f;
        
        _defaultTheme = [hullColor, 1f, compColor, 1f, _masterAlarmMat.GetColor("_Color") * 255f,
            _masterAlarmMat.GetColor("_EmissionColor") * 191f, 1f, _masterAlarmLight.color * 255f];

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
            theme.AlarmColor, theme.AlarmLitColor, theme.AlarmLitIntensity, theme.IndicatorLight];
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
        var newLight = Color.Lerp((Color)start[7], (Color)end[7], lerp);

        return [newHull, newHullIntensity, newComp, newCompIntensity,
            newAlarm, newAlarmLit, newAlarmIntensity, newLight];
    }

    protected override void SetColor(Color color)
    {
        SetColor([_defaultTheme[0], 1f, color, color, 1f, color]);
    }

    protected override void SetColor(List<object> theme)
    {
        _damageScreenMat.SetColor("_DamagedHullFill", (Color)theme[0] / 191f * (float)theme[1] * (float)theme[1]);
        _damageScreenMat.SetColor("_DamagedComponentFill", (Color)theme[2] / 191f * (float)theme[3] * (float)theme[3]);
        _masterAlarmMat.SetColor("_Color", (Color)theme[4] / 255f);
        //_masterAlarmMat.SetColor("_EmissionColor", (Color)theme[5] / 191f * (float)theme[6] * (float)theme[6]);
        _cockpitUI._damageLightColor = (Color)theme[7] / 255f;
        _masterAlarmLight.color = (Color)theme[7] / 255f;
        foreach (DamageEffect effect in _damageEffects)
        {
            if (effect._damageLight)
            {
                effect._damageLight._light.color = (Color)theme[7] / 255f;
            }
            if (effect._damageLightRenderer)
            {
                //effect._damageLightRenderer.SetColor(theme[4]);
                effect._damageLightRendererColor = (Color)theme[5] / 191f * (float)theme[6] * (float)theme[6];
            }
        }
    }

    protected override void ResetColor()
    {
        SetColor(_defaultTheme);
        base.ResetColor();
    }
}
