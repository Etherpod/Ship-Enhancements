using System.Collections.Generic;
using System.Linq;
using ShipEnhancements.Utils;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class PlantSelectorModule : SelectorModule
{
	[SerializeField]
	private GameObject[] _plantPrefabs;

	private GameObject _defaultPlant;
	private GameObject _currentPlant;

	private void Start()
	{
		_defaultPlant = SELocator.GetShipTransform()
			.Find("Module_Cockpit/Props_Cockpit/Props_HEA_ShipFoliage").gameObject;

		for (int i = 0; i < _plantPrefabs.Length; i++)
		{
			if (_plantPrefabs[i] != null)
			{
				AssetBundleUtilities.ReplaceShaders(_plantPrefabs[i]);

				if (_optionNames[i] == "Cactus")
				{
					_plantPrefabs[i].transform.Find("Props_HGT_Cactus_Single_A_Alt/DethornedCactus").gameObject
						.SetActive(!(bool)Settings.disableHazardPrevention.GetProperty());
					_plantPrefabs[i].transform.Find("Props_HGT_Cactus_Single_A_Alt/ThornedCactus").gameObject
						.SetActive((bool)Settings.disableHazardPrevention.GetProperty());
				}
			}
		}
	}

	protected override OptionsListElementData[] GenerateOptionData()
	{
		List<OptionsListElementData> data = [];
		for (int i = 0; i < _optionNames.Length; i++)
		{
			if (i >= _plantPrefabs.Length) break;
			data.Add(new PlantSelectorOptionData(i, _optionNames[i], _plantPrefabs[i]));
		}

		return data.ToArray();
	}
	
	protected override void OnSelectOption(OptionsListElementData data)
	{
		if (data is not PlantSelectorOptionData plantData) return;
		
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
	}

	public override float GetSelectionFadeOverride() => 0f;
}

public class PlantSelectorOptionData : OptionsListElementData
{
	public GameObject plantPrefab;

	public PlantSelectorOptionData(int index, string name, GameObject prefab) : base(index, name)
	{
		plantPrefab = prefab;
	}
}