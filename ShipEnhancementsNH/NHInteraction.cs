using UnityEngine;
using ShipEnhancements;
using HarmonyLib;
using System.Reflection;

namespace ShipEnhancementsNH;

public class NHInteraction : MonoBehaviour, INHInteraction
{
    private void Start()
    {
        ShipEnhancements.ShipEnhancements.Instance.AssignNHInterface(this);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }
}