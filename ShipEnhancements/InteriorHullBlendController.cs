using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class InteriorHullBlendController : ShipHullBlendController
{
    protected override string CurrentBlend => (string)interiorHullColorBlend.GetProperty();
    protected override int NumberOfOptions => int.Parse((string)interiorHullColorOptions.GetProperty());
    protected override string OptionStem => "interiorHullColor";
    protected override RenderTexture TargetRenderTex => ShipEnhancements.Instance.interiorHullRenderTex;
}
