using UnityEngine;

namespace ShipEnhancements;

public class ShipWarpCoreComponent : ShipComponent
{
    [SerializeField]
    private ShipWarpCoreController _warpController;
    [SerializeField]
    private OWRenderer _blackHoleRenderer;
    [SerializeField]
    private OWLight2 _blackHoleLight;

    private readonly int _maxFrameDelay = 5;
    private int _frameDelay;

    private void Start()
    {
        _componentName = ShipEnhancements.Instance.WarpCoreName;
        enabled = false;
    }

    private void Update()
    {
        if (_frameDelay > 0)
        {
            _frameDelay--;
        }
        else
        {
            _blackHoleRenderer.SetActivation(!_blackHoleRenderer.IsActive());
            _blackHoleLight.SetIntensityScale(_blackHoleRenderer.IsActive() ? 1f : 0.5f);
            _frameDelay = Random.Range(0, _maxFrameDelay + 1);
        }
    }

    public override void OnComponentDamaged()
    {
        _warpController.SetDamaged(true);
        _frameDelay = Random.Range(0, _maxFrameDelay + 1);
        enabled = true;
    }

    public override void OnComponentRepaired()
    {
        _warpController.SetDamaged(false);
        _blackHoleRenderer.SetActivation(true);
        _blackHoleLight.SetIntensityScale(1f);
        enabled = false;
    }
}
