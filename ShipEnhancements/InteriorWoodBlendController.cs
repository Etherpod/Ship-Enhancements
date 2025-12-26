using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class InteriorWoodBlendController : ShipHullBlendController
{
    protected override string CurrentBlend => (string)interiorWoodColorBlend.GetProperty();
    protected override int NumberOfOptions => int.Parse((string)interiorWoodColorOptions.GetProperty());
    protected override string OptionStem => "interiorWoodColor";
}