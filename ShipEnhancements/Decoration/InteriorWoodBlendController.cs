using UnityEngine;
using static ShipEnhancements.Settings;

namespace ShipEnhancements.Decoration;

public class InteriorWoodBlendController : ShipHullBlendController
{
    protected override string CurrentBlend => (string)interiorWoodColorBlend.GetProperty();
    protected override int NumberOfOptions => int.Parse((string)interiorWoodColorOptions.GetProperty());
    protected override string OptionStem => "interiorWoodColor";
}