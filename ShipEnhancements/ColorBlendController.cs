using UnityEngine;
using System.Collections.Generic;

namespace ShipEnhancements;

public class ColorBlendController : MonoBehaviour
{
    protected virtual string CurrentBlend { get; }
    protected virtual int NumberOfOptions { get; }
    protected virtual string OptionStem { get; }

    protected float _rainbowCycleLength = 15f;
    protected float _currentLerp;
    protected float _targetLerp;
    protected float _resetFadeStartTime;
    protected bool _reset = false;

    protected Color[] _blendColors;
    protected List<object>[] _blendThemes;
    protected string _blendMode;
    protected List<int> _rainbowIndexes = [];
    protected Color _defaultColor;
    protected Color _cachedColor;

    protected readonly float _maxLerpStep = 0.005f;
    protected readonly float _resetFadeTime = 2f;

    protected virtual void Awake()
    {
        if (CurrentBlend == "Ship Damage %")
        {
            SELocator.GetShipDamageController().OnDamageUpdated += OnDamageUpdated;
        }

        if ((bool)ShipEnhancements.Settings.enableColorBlending.GetProperty() && NumberOfOptions > 1)
        {
            _blendColors = new Color[NumberOfOptions];
            _blendThemes = new List<object>[NumberOfOptions];

            for (int i = 0; i < _blendThemes.Length; i++)
            {
                var setting = (string)(OptionStem + (i + 1))
                    .AsEnum<ShipEnhancements.Settings>().GetProperty();
                if (setting == "Rainbow")
                {
                    _rainbowIndexes.Add(i);
                    _blendColors[i] = Color.white;
                    // add rainbow to color themes
                }
                else if (setting == "Default")
                {
                    _blendColors[i] = _defaultColor;
                    SetBlendTheme(i, setting);
                    // add default to color themes
                }
                else
                {
                    _blendColors[i] = GetThemeColor(setting);
                    SetBlendTheme(i, setting);
                }
            }
            _blendMode = CurrentBlend;
        }
    }

    protected virtual Color GetThemeColor(string themeName)
    {
        return Color.white;
    }

    protected virtual void SetBlendTheme(int i, string themeName) { }

    protected virtual void SetColor(Color color) { }

    protected void SetColor(ColorHSV color)
    {
        SetColor(color.AsRGB());
    }

    protected virtual void UpdateLerp(int start, int end, float lerp) { }

    private void Update()
    {
        if (_blendColors == null) return;

        if (_blendMode == "Temperature" && SELocator.GetShipTemperatureDetector() != null)
        {
            _targetLerp = (SELocator.GetShipTemperatureDetector().GetTemperatureRatio() + 1f) / 2f;
            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                /*Color color = Color.Lerp(_blendColors[1], _blendColors[0], _currentLerp);
                SetColor(color);*/
                UpdateLerp(1, 0, _currentLerp);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                if (_currentLerp < 0.5f)
                {
                    color = Color.Lerp(_blendColors[2], _blendColors[1], _currentLerp * 2f);
                }
                else
                {
                    color = Color.Lerp(_blendColors[1], _blendColors[0], (_currentLerp - 0.5f) * 2f);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Ship Temperature" && SELocator.GetShipTemperatureDetector() != null)
        {
            _targetLerp = SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio();
            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[1], _blendColors[0], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                if (_currentLerp < 0.5f)
                {
                    color = Color.Lerp(_blendColors[2], _blendColors[1], _currentLerp * 2f);
                }
                else
                {
                    color = Color.Lerp(_blendColors[1], _blendColors[0], (_currentLerp - 0.5f) * 2f);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Reactor State")
        {
            ShipReactorComponent reactor = SELocator.GetShipDamageController()._shipReactorComponent;
            bool enabled = reactor._damaged && SELocator.GetShipTransform().Find("Module_Engine") != null;
            _targetLerp = enabled
                ? 1f - reactor._criticalTimer / reactor._criticalCountdown
                : 0f;

            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                _cachedColor = Color.Lerp(_blendColors[1], _blendColors[2], _currentLerp);

                if (!SELocator.GetShipDamageController().IsReactorCritical())
                {
                    if (!_reset)
                    {
                        _reset = true;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    color = Color.Lerp(_cachedColor, _blendColors[0], timeLerp);
                }
                else
                {
                    if (_reset)
                    {
                        _reset = false;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    color = Color.Lerp(_blendColors[0], _cachedColor, timeLerp);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Ship Damage %")
        {
            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                _cachedColor = Color.Lerp(_blendColors[1], _blendColors[2], _currentLerp);
                if (_targetLerp == 0)
                {
                    if (!_reset)
                    {
                        _reset = true;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    color = Color.Lerp(_cachedColor, _blendColors[0], timeLerp);
                }
                else
                {
                    if (_reset)
                    {
                        _reset = false;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    color = Color.Lerp(_blendColors[0], _cachedColor, timeLerp);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Fuel")
        {
            _targetLerp = 1 - SELocator.GetShipResources().GetFractionalFuel();

            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                if (_currentLerp < 0.5f)
                {
                    color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp * 2f);
                }
                else
                {
                    color = Color.Lerp(_blendColors[1], _blendColors[2], (_currentLerp - 0.5f) * 2f);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Oxygen")
        {
            _targetLerp = 1 - SELocator.GetShipResources().GetFractionalOxygen();

            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                if (_currentLerp < 0.5f)
                {
                    color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp * 2f);
                }
                else
                {
                    color = Color.Lerp(_blendColors[1], _blendColors[2], (_currentLerp - 0.5f) * 2f);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Velocity")
        {
            ReferenceFrame rf = Locator.GetReferenceFrame();
            if (rf == null || rf.GetOWRigidBody() == null)
            {
                _targetLerp = 0.5f;
            }
            else
            {
                Vector3 relative = rf.GetOWRigidBody().GetRelativeVelocity(SELocator.GetShipBody());
                Vector3 toTarget = SELocator.GetShipBody().GetWorldCenterOfMass() - rf.GetPosition();
                float speed = Vector3.Dot(relative, toTarget.normalized);

                float lower = Mathf.InverseLerp(-100f, -10f, speed) * 0.5f;
                float upper = Mathf.InverseLerp(10f, 100f, speed) * 0.5f;
                _targetLerp = Mathf.Clamp01(lower + upper);
            }

            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[1], _blendColors[0], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                if (_currentLerp < 0.5f)
                {
                    color = Color.Lerp(_blendColors[0], _blendColors[1], _currentLerp * 2f);
                }
                else
                {
                    color = Color.Lerp(_blendColors[1], _blendColors[2], (_currentLerp - 0.5f) * 2f);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Gravity")
        {
            AlignmentForceDetector detector = SELocator.GetShipDetector().GetComponent<AlignmentForceDetector>();
            Vector3 acceleration = detector.GetForceAcceleration();
            _targetLerp = Mathf.InverseLerp(0f, 50f, acceleration.magnitude);

            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendColors.Length == 2)
            {
                Color color = Color.Lerp(_blendColors[1], _blendColors[0], _currentLerp);
                SetColor(color);
            }
            else if (_blendColors.Length == 3)
            {
                Color color;
                _cachedColor = Color.Lerp(_blendColors[1], _blendColors[2], _currentLerp);

                if (detector.GetAlignmentAcceleration().sqrMagnitude == 0f)
                {
                    if (!_reset)
                    {
                        _reset = true;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    color = Color.Lerp(_cachedColor, _blendColors[0], timeLerp);
                }
                else
                {
                    if (_reset)
                    {
                        _reset = false;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    color = Color.Lerp(_blendColors[0], _cachedColor, timeLerp);
                }
                SetColor(color);
            }
        }
        else if (_blendMode == "Time")
        {
            var t = Time.time / 4f / _blendColors.Length % _blendColors.Length;
            var a = (int)t;
            var b = (a + 1) % _blendColors.Length;
            /*var color = Color.Lerp(_blendColors[a], _blendColors[b],
                Mathf.SmoothStep(0, 1, (t - a) * 2f - 0.5f));
            SetColor(color);*/
            UpdateLerp(a, b, Mathf.SmoothStep(0, 1, (t - a) * 2f - 0.5f));
        }
        else
        {
            ResetColor();
        }
    }

    protected virtual void ResetColor()
    {
        _blendColors = null;
        enabled = false;
    }

    private void FixedUpdate()
    {
        if (_blendColors != null && _rainbowIndexes.Count == 0)
        {
            return;
        }

        float num = Time.time % _rainbowCycleLength / _rainbowCycleLength;
        ColorHSV color = new ColorHSV(num, 1f, 255f);

        if (_blendColors != null)
        {
            foreach (int i in _rainbowIndexes)
            {
                ShipEnhancements.WriteDebugMessage("pool rainbow");
                _blendColors[i] = color.AsRGB();
            }
        }
        else
        {
            ShipEnhancements.WriteDebugMessage("lone rainbow");
            SetColor(color);
        }
    }

    private void OnDamageUpdated()
    {
        int numParts = 0;
        int numDamaged = 0;

        foreach (var hull in SELocator.GetShipDamageController()._shipHulls)
        {
            numParts++;
            if (hull.isDamaged)
            {
                numDamaged++;
            }
        }
        foreach (var comp in SELocator.GetShipDamageController()._shipComponents)
        {
            numParts++;
            if (comp.isDamaged)
            {
                numDamaged++;
            }
        }

        _targetLerp = (float)numDamaged / numParts;
    }

    private void OnDestroy()
    {
        if (CurrentBlend == "Ship Damage %")
        {
            SELocator.GetShipDamageController().OnDamageUpdated -= OnDamageUpdated;
        }
    }
}
