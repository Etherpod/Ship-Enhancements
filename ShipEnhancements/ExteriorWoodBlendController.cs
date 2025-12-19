using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ExteriorWoodBlendController : ShipHullBlendController
{
    protected override string CurrentBlend => (string)exteriorWoodColorBlend.GetProperty();
    protected override int NumberOfOptions => int.Parse((string)exteriorWoodColorOptions.GetProperty());
    protected override string OptionStem => "exteriorWoodColor";
    protected override RenderTexture TargetRenderTex => ShipEnhancements.Instance.exteriorWoodRenderTex;
    protected override bool IsWoodController => true;
}