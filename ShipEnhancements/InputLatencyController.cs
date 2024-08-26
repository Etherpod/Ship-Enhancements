using UnityEngine;
using System.Collections.Generic;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public static class InputLatencyController
{
    private static readonly Queue<(float, Vector3)> _translationalInputs = new();
    private static readonly Queue<(float, Vector3)> _rotationalInputs = new();
    private static ShipThrusterModel _shipThrusterModel;

    public static void Initialize()
    {
        _shipThrusterModel = Locator.GetShipBody().GetComponent<ShipThrusterModel>();
    }

    public static void FixedUpdate()
    {
        if (_translationalInputs.Count > 0)
        {
            var nextInput = _translationalInputs.Peek();
            if (Time.time > nextInput.Item1 + (float)shipInputLatency.GetProperty())
            {
                _shipThrusterModel._translationalInput += nextInput.Item2;
                _translationalInputs.Dequeue();
            }
        }
        if (_rotationalInputs.Count > 0)
        {
            var nextInput = _rotationalInputs.Peek();
            if (Time.time > nextInput.Item1 + (float)shipInputLatency.GetProperty())
            {
                _shipThrusterModel._rotationalInput += nextInput.Item2;
                _rotationalInputs.Dequeue();
            }
        }
    }

    public static void AddTranslationalInput(Vector3 input)
    {
        _translationalInputs.Enqueue((Time.time, input));
    }

    public static void AddRotationalInput(Vector3 input)
    {
        _rotationalInputs.Enqueue((Time.time, input));
    }
}
