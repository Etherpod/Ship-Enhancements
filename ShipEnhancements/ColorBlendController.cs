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

    protected List<object>[] _blendThemes;
    protected string _blendMode;
    protected List<int> _rainbowIndexes = [];
    protected List<object> _defaultTheme;
    protected List<object> _cachedTheme;
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
            _blendThemes = new List<object>[NumberOfOptions];

            for (int i = 0; i < _blendThemes.Length; i++)
            {
                var setting = (string)(OptionStem + (i + 1))
                    .AsEnum<ShipEnhancements.Settings>().GetProperty();

                if (setting == "Rainbow")
                {
                    _rainbowIndexes.Add(i);
                }

                SetBlendTheme(i, setting);
            }
            _blendMode = CurrentBlend;
        }
    }

    protected virtual void SetBlendTheme(int i, string themeName) { }

    protected virtual void SetColor(Color color) { }

    protected virtual void SetColor(List<object> theme) { }

    protected void UpdateLerp(int start, int end, float lerp) 
    {
        UpdateLerp(_blendThemes[start], _blendThemes[end], lerp);
    }

    protected virtual void UpdateLerp(List<object> start, List<object> end, float lerp) { }

    protected virtual List<object> GetLerp(List<object> start, List<object> end, float lerp)
    {
        return null;
    }

    protected virtual void UpdateRainbowTheme(int index, Color color) { }

    private void Update()
    {
        if (_blendThemes == null) return;

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

            if (_blendThemes.Length == 2)
            {
                UpdateLerp(1, 0, _currentLerp);
            }
            else if (_blendThemes.Length == 3)
            {
                if (_currentLerp < 0.5f)
                {
                    UpdateLerp(2, 1, _currentLerp * 2f);
                }
                else
                {
                    UpdateLerp(1, 0, (_currentLerp - 0.5f) * 2f);
                }
            }
        }
        else if (_blendMode == "Ship Temperature" && SELocator.GetShipTemperatureDetector() != null)
        {
            _targetLerp = (SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio() + 1f) / 2f;
            if (Mathf.Abs(_targetLerp - _currentLerp) > _maxLerpStep)
            {
                _currentLerp = Mathf.Lerp(_currentLerp, _targetLerp, Time.deltaTime);
            }
            else
            {
                _currentLerp = _targetLerp;
            }

            if (_blendThemes.Length == 2)
            {
                UpdateLerp(1, 0, _currentLerp);
            }
            else if (_blendThemes.Length == 3)
            {
                if (_currentLerp < 0.5f)
                {
                    UpdateLerp(2, 1, _currentLerp * 2f);
                }
                else
                {
                    UpdateLerp(1, 0, (_currentLerp - 0.5f) * 2f);
                }
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

            if (_blendThemes.Length == 2)
            {
                UpdateLerp(1, 0, _currentLerp);
            }
            else if (_blendThemes.Length == 3)
            {
                _cachedTheme = GetLerp(_blendThemes[1], _blendThemes[2], _currentLerp);

                if (!SELocator.GetShipDamageController().IsReactorCritical())
                {
                    if (!_reset)
                    {
                        _reset = true;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    UpdateLerp(_cachedTheme, _blendThemes[0], timeLerp);
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

                    UpdateLerp(_blendThemes[0], _cachedTheme, timeLerp);
                }            
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

            if (_blendThemes.Length == 2)
            {
                UpdateLerp(0, 1, _currentLerp);
            }
            else if (_blendThemes.Length == 3)
            {
                _cachedTheme = GetLerp(_blendThemes[1], _blendThemes[2], _currentLerp);

                if (_targetLerp == 0)
                {
                    if (!_reset)
                    {
                        _reset = true;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    UpdateLerp(_cachedTheme, _blendThemes[0], timeLerp);
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

                    UpdateLerp(_blendThemes[0], _cachedTheme, timeLerp);
                }
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

            if (_blendThemes.Length == 2)
            {
                UpdateLerp(0, 1, _currentLerp);
            }
            else if (_blendThemes.Length == 3)
            {
                if (_currentLerp < 0.5f)
                {
                    UpdateLerp(0, 1, _currentLerp * 2f);
                }
                else
                {
                    UpdateLerp(1, 2, (_currentLerp - 0.5f) * 2f);
                }
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

            if (_blendThemes.Length == 2)
            {
                UpdateLerp(0, 1, _currentLerp);
            }
            else if (_blendThemes.Length == 3)
            {
                if (_currentLerp < 0.5f)
                {
                    UpdateLerp(0, 1, _currentLerp * 2f);
                }
                else
                {
                    UpdateLerp(1, 2, (_currentLerp - 0.5f) * 2f);
                }
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

            if (_blendThemes.Length == 2)
            {
                UpdateLerp(1, 0, _currentLerp);
            }
            else if (_blendThemes.Length == 3)
            {
                if (_currentLerp < 0.5f)
                {
                    UpdateLerp(0, 1, _currentLerp * 2f);
                }
                else
                {
                    UpdateLerp(1, 2, (_currentLerp - 0.5f) * 2f);
                }
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

            if (_blendThemes.Length == 2)
            {
                UpdateLerp(1, 0, _currentLerp);
            }
            else if (_blendThemes.Length == 3)
            {
                _cachedTheme = GetLerp(_blendThemes[1], _blendThemes[2], _currentLerp);

                if (detector.GetAlignmentAcceleration().sqrMagnitude == 0f)
                {
                    if (!_reset)
                    {
                        _reset = true;
                        _resetFadeStartTime = Time.time;
                    }
                    float timeLerp = Mathf.InverseLerp(_resetFadeStartTime,
                        _resetFadeStartTime + _resetFadeTime, Time.time);

                    UpdateLerp(_cachedTheme, _blendThemes[0], timeLerp);
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

                    UpdateLerp(_blendThemes[0], _cachedTheme, timeLerp);
                }
            }
        }
        else if (_blendMode == "Time")
        {
            var t = Time.time / 4f / _blendThemes.Length % _blendThemes.Length;
            var a = (int)t;
            var b = (a + 1) % _blendThemes.Length;
            UpdateLerp(a, b, Mathf.SmoothStep(0, 1, (t - a) * 2f - 0.5f));
        }
        else
        {
            ResetColor();
        }
    }

    protected virtual void ResetColor()
    {
        _blendThemes = null;
        enabled = false;
    }

    private void FixedUpdate()
    {
        if (_blendThemes != null && _rainbowIndexes.Count == 0)
        {
            return;
        }

        float num = Time.time % _rainbowCycleLength / _rainbowCycleLength;
        ColorHSV color = new ColorHSV(num, 1f, 255f);
        Color rgbColor = color.AsRGB();
        rgbColor.a = 255f;

        if (_blendThemes != null)
        {
            foreach (int i in _rainbowIndexes)
            {
                UpdateRainbowTheme(i, rgbColor);
            }
        }
        else
        {
            SetColor(rgbColor);
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
