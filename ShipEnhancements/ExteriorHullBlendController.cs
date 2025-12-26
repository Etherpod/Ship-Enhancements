using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class ExteriorHullBlendController : ShipHullBlendController
{
    protected override string CurrentBlend => (string)exteriorHullColorBlend.GetProperty();
    protected override int NumberOfOptions => int.Parse((string)exteriorHullColorOptions.GetProperty());
    protected override string OptionStem => "exteriorHullColor";
}