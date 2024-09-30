using System;

namespace ShipEnhancements;

public class RandomSettingValue
{
    object[] _possibleValues = [];
    (object, object)[] _possibleRanges = [];
    float _minValue = -1;
    float _maxValue = -1;
    float _randomChance = -1;
    object _defaultValue;

    public RandomSettingValue(float randomChance)
    {
        _randomChance = randomChance;
    }

    public RandomSettingValue(object[] possibleValues)
    {
        _possibleValues = possibleValues;
    }

    public RandomSettingValue(float minValue, float maxValue)
    {
        _minValue = minValue;
        _maxValue = maxValue;
    }

    public RandomSettingValue(float minValue, float maxValue, float randomChance, float defaultValue)
    {
        _minValue = minValue;
        _maxValue = maxValue;
        _randomChance = randomChance;
        _defaultValue = defaultValue;
    }

    public RandomSettingValue(object[] possibleValues, float randomChance, object defaultValue)
    {
        _possibleValues = possibleValues;
        _randomChance = randomChance;
        _defaultValue = defaultValue;
    }

    public RandomSettingValue((object, object)[] possibleRanges)
    {
        _possibleRanges = possibleRanges;
    }

    public RandomSettingValue((object, object)[] possibleRanges, float randomChance, object defaultValue)
    {
        _possibleRanges = possibleRanges;
        _randomChance = randomChance;
        _defaultValue = defaultValue;
    }

    public object GetRandomValue()
    {
        if (_possibleValues.Length > 0)
        {
            if (_randomChance > 0 && UnityEngine.Random.value > _randomChance)
            {
                return _defaultValue;
            }

            int index = UnityEngine.Random.Range(0, _possibleValues.Length);
            return _possibleValues[index];
        }
        else if (_possibleRanges.Length > 0)
        {
            if (_randomChance > 0 && UnityEngine.Random.value > _randomChance)
            {
                return _defaultValue;
            }

            int index = UnityEngine.Random.Range(0, _possibleRanges.Length);
            (object, object) range = _possibleRanges[index];
            if (range.Item1 is float f1 && range.Item2 is float f2)
            {
                return UnityEngine.Random.Range(f1, f2);
            }
            else
            {
                return UnityEngine.Random.value < 0.5f ? range.Item1 : range.Item2;
            }
        }
        else if (_maxValue > 0)
        {
            if (_randomChance > 0 && UnityEngine.Random.value > _randomChance)
            {
                return _defaultValue;
            }

            return UnityEngine.Random.Range(_minValue, _maxValue);
        }
        else
        {
            return UnityEngine.Random.value <= _randomChance;
        }
    }
}
