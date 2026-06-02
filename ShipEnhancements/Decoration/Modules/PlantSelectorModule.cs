using System.Collections.Generic;
using ShipEnhancements.Utils;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class PlantSelectorModule : SelectorModule
{
	[SerializeField]
	private GameObject[] _plantPrefabs;

	private GameObject _defaultPlant;
	private string  _currentName;
	private GameObject _currentPlant;

	private void Start()
	{
		_defaultPlant = SELocator.GetShipTransform()
			.Find("Module_Cockpit/Props_Cockpit/Props_HEA_ShipFoliage").gameObject;

		foreach (var prefab in _plantPrefabs)
		{
			if (prefab != null)
			{
				AssetBundleUtilities.ReplaceShaders(prefab);
			}
		}
	}

	protected override OptionsListElementData[] GenerateOptionData()
	{
		List<OptionsListElementData> data = [];
		for (int i = 0; i < _optionNames.Length; i++)
		{
			if (i >= _plantPrefabs.Length) break;
			data.Add(new PlantSelectorOptionData(_optionNames[i], _plantPrefabs[i]));
		}

		return data.ToArray();
	}

	protected override void OnSubmitOption(OptionsListElementData data)
	{
		if (data is not PlantSelectorOptionData plantData ||
			plantData.displayName == _currentName) return;
		
		if (_currentPlant != null)
		{
			Destroy(_currentPlant);
			_currentPlant = null;
		}

		if (plantData.displayName == "Default")
		{
			_defaultPlant.SetActive(true);
		}
		else if (plantData.plantPrefab != null)
		{
			_defaultPlant.SetActive(false);
			
			_currentPlant = ShipEnhancements.CreateObject(plantData.plantPrefab, 
					_defaultPlant.transform.parent);
		}
		else
		{
			_defaultPlant.SetActive(false);
		}

		_currentName = plantData.displayName;
	}

	public override float GetSelectionFadeOverride() => 0f;
}

public class PlantSelectorOptionData : OptionsListElementData
{
	public GameObject plantPrefab;

	public PlantSelectorOptionData(string name, GameObject prefab) : base(name)
	{
		plantPrefab = prefab;
	}
}