using System;
using UnityEngine;

namespace ShipEnhancements;

public class RandomSettingValue
{
    (object value, (float minWeight, float maxWeight) weightRange)[] _possibleValues = [];
    float _minValue = -1;
    float _maxValue = -1;
    float _minRandomChance = -1;
    float _maxRandomChance = -1;
    object _defaultValue;

    public RandomSettingValue((float minChance, float maxChance) chanceRange)
    {
        _minRandomChance = chanceRange.minChance;
        _maxRandomChance = chanceRange.maxChance;
    }

    public RandomSettingValue((float minValue, float maxValue) valueRange, (float minChance, float maxChance) chanceRange, float defaultValue)
    {
        _minValue = valueRange.minValue;
        _maxValue = valueRange.maxValue;
        _minRandomChance = chanceRange.minChance;
        _maxRandomChance = chanceRange.maxChance;
        _defaultValue = defaultValue;
    }

    public RandomSettingValue((object, (float, float))[] possibleValues, (float minChance, float maxChance) chanceRange, object defaultValue)
    {
        _possibleValues = possibleValues;
        _minRandomChance = chanceRange.minChance;
        _maxRandomChance = chanceRange.maxChance;
        _defaultValue = defaultValue;
    }

    public float GetRandomChance()
    {
        float lerp = (float)ShipEnhancements.Settings.randomDifficulty.GetProperty();
        return Mathf.Lerp(_minRandomChance, _maxRandomChance, lerp); 
    }

    public object GetRandomValue(bool ignoreChance = false)
    {
        float lerp = (float)ShipEnhancements.Settings.randomDifficulty.GetProperty();
        float chance = Mathf.Lerp(_minRandomChance, _maxRandomChance, lerp);

        if (_possibleValues.Length > 0)
        {
            if (!ignoreChance && chance > 0 && UnityEngine.Random.value > chance)
            {
                return _defaultValue;
            }

            float total = 0;
            for (int i = 0; i < _possibleValues.Length; i++)
            {
                float weight = Mathf.Lerp(_possibleValues[i].weightRange.minWeight, _possibleValues[i].weightRange.maxWeight, lerp);
                total += weight;
            }
            float rand = UnityEngine.Random.Range(0f, total);
            float sum = 0;
            for (int i = 0; i < _possibleValues.Length; i++)
            {
                float weight = Mathf.Lerp(_possibleValues[i].weightRange.minWeight, _possibleValues[i].weightRange.maxWeight, lerp);
                sum += weight;
                if (rand < sum)
                {
                    object value = _possibleValues[i].value;
                    if (value is float)
                    {
                        return (float)value;
                    }
                    else if (value is (float, float))
                    {
                        (float min, float max) range = ((float, float))value;
                        return UnityEngine.Random.Range(range.min, range.max);
                    }
                    else if (value is string)
                    {
                        return value.ToString();
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            return _defaultValue;
        }
        else if (_maxValue > 0)
        {
            if (!ignoreChance && chance > 0 && UnityEngine.Random.value > chance)
            {
                return _defaultValue;
            }

            return UnityEngine.Random.Range(_minValue, _maxValue);
        }
        else
        {
            return ignoreChance || UnityEngine.Random.value <= chance;
        }
    }
}
