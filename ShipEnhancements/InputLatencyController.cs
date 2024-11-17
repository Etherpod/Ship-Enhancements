using UnityEngine;
using System.Collections.Generic;
using static ShipEnhancements.ShipEnhancements.Settings;
using System.Linq;

namespace ShipEnhancements;

public static class InputLatencyController
{
    public static readonly Queue<(float, Vector3)> _translationalInputs = new();
    private static readonly Queue<(float, Vector3)> _rotationalInputs = new();
    private static ShipThrusterModel _shipThrusterModel;
    private static ShipThrusterController _shipThrusterController;

    private static readonly Queue<(float, Vector3)> _savedTranslationalInputs = new();
    private static readonly Queue<(float, Vector3)> _savedRotationalInputs = new();

    public static bool ReadingSavedInputs { get; private set; }

    public static bool IsTranslationalInputQueued => _translationalInputs.Count > 0;
    public static bool IsRotationalInputQueued => _rotationalInputs.Count > 0;
    public static bool IsInputQueued => IsTranslationalInputQueued || IsRotationalInputQueued;

    public static bool HasSavedInputs => _savedTranslationalInputs.Count > 0 || _savedRotationalInputs.Count > 0;

    public static void Initialize()
    {
        _shipThrusterModel = SELocator.GetShipBody().GetComponent<ShipThrusterModel>();
        _shipThrusterController = SELocator.GetShipBody().GetComponent<ShipThrusterController>();

        if (HasSavedInputs && (float)shipInputLatency.GetProperty() < 0f)
        {
            _savedTranslationalInputs.ToList().ForEach(_translationalInputs.Enqueue);
            _savedRotationalInputs.ToList().ForEach(_rotationalInputs.Enqueue);
            _savedTranslationalInputs.Clear();
            _savedRotationalInputs.Clear();
            ReadingSavedInputs = true;
        }
        else
        {
            ReadingSavedInputs = false;
        }
    }

    public static void OnUnloadScene()
    {
        _translationalInputs.Clear();
        _rotationalInputs.Clear();
        _shipThrusterModel = null;
        _shipThrusterController = null;
    }

    public static void ProcessInputs()
    {
        bool run = false;
        if (_translationalInputs.Count > 0)
        {
            run = !_shipThrusterController.enabled;
            var nextInput = _translationalInputs.Peek();
            if (Time.timeSinceLevelLoad > nextInput.Item1 + (float)shipInputLatency.GetProperty())
            {
                _shipThrusterController._translationalInput = nextInput.Item2;
                _translationalInputs.Dequeue();
            }
        }
        if (_rotationalInputs.Count > 0)
        {
            run = !_shipThrusterController.enabled || run;
            var nextInput = _rotationalInputs.Peek();
            if (Time.timeSinceLevelLoad > nextInput.Item1 + (float)shipInputLatency.GetProperty())
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

    public static void ProcessSavedInputs()
    {
        bool run = false;
        if (_translationalInputs.Count > 0)
        {
            run = !_shipThrusterController.enabled;
            var nextInput = _translationalInputs.Peek();
            if (Time.timeSinceLevelLoad > nextInput.Item1)
            {
                _shipThrusterController._translationalInput = nextInput.Item2;
                _translationalInputs.Dequeue();
            }
            else
            {
                _shipThrusterController._translationalInput = Vector3.zero;
            }
        }
        if (_rotationalInputs.Count > 0)
        {
            run = !_shipThrusterController.enabled || run;
            var nextInput = _rotationalInputs.Peek();
            if (Time.timeSinceLevelLoad > nextInput.Item1)
            {
                _shipThrusterController._rotationalInput = nextInput.Item2;
                _rotationalInputs.Dequeue();
            }
            else
            {
                _shipThrusterController._rotationalInput = Vector3.zero;
            }
        }
        if (run)
        {
            _shipThrusterController.FixedUpdate();
        }
    }

    public static void AddTranslationalInput(Vector3 input)
    {
        _translationalInputs.Enqueue((Time.timeSinceLevelLoad, input));
    }

    public static void AddRotationalInput(Vector3 input)
    {
        _rotationalInputs.Enqueue((Time.timeSinceLevelLoad, input));
    }

    public static void SaveTranslationalInput(Vector3 input)
    {
        _savedTranslationalInputs.Enqueue((Time.timeSinceLevelLoad, input));
    }

    public static void SaveRotationalInput(Vector3 input)
    {
        _savedRotationalInputs.Enqueue((Time.timeSinceLevelLoad, input));
    }
}
