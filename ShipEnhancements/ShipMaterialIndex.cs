using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements;

public class ShipMaterialIndex : MonoBehaviour
{
    [SerializeField]
    Material[] _exteriorMaterials;
    [SerializeField]
    Material[] _interiorMaterials;

    public Dictionary<Material, Material> GetMaterialDictionary()
    {
        Dictionary<Material, Material> dict = [];
        for (int i = 0; i < Mathf.Min(_exteriorMaterials.Length, _interiorMaterials.Length); i++)
        {
            dict.Add(_exteriorMaterials[i], _interiorMaterials[i]);
        }
        return dict;
    }
}
