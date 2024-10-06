using UnityEngine;
using System.Collections.Generic;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public static class InputLatencyController
{
    private static readonly Queue<(float, Vector3)> _translationalInputs = new();
    private static readonly Queue<(float, Vector3)> _rotationalInputs = new();
    private static ShipThrusterModel _shipThrusterModel;
    private static ShipThrusterController _shipThrusterController;

    public static void Initialize()
    {
        _shipThrusterModel = Locator.GetShipBody().GetComponent<ShipThrusterModel>();
        _shipThrusterController = Locator.GetShipBody().GetComponent<ShipThrusterController>();
    }

    public static void FixedUpdate()
    {
        bool run = false;
        if (_translationalInputs.Count > 0)
        {
            run = !_shipThrusterController.enabled;
            var nextInput = _translationalInputs.Peek();
            if (Time.time > nextInput.Item1 + (float)shipInputLatency.GetProperty())
            {
                _shipThrusterController._translationalInput = nextInput.Item2;
                _translationalInputs.Dequeue();
            }
        }
        if (_rotationalInputs.Count > 0)
        {
            run = !_shipThrusterController.enabled;
            var nextInput = _rotationalInputs.Peek();
            if (Time.time > nextInput.Item1 + (float)shipInputLatency.GetProperty())
            {
                _shipThrusterController._rotationalInput = nextInput.Item2;
                _rotationalInputs.Dequeue();
            }
        }
        if (run)
        {
            _shipThrusterController.FixedUpdate();
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
