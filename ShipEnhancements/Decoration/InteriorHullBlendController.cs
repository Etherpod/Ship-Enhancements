using UnityEngine;
using static ShipEnhancements.Settings;

namespace ShipEnhancements.Decoration;

public class InteriorHullBlendController : ShipHullBlendController
{
    protected override string CurrentBlend => (string)interiorHullColorBlend.GetProperty();
    protected override int NumberOfOptions => int.Parse((string)interiorHullColorOptions.GetProperty());
    protected override string OptionStem => "interiorHullColor";
}
