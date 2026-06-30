using System.Collections.Generic;
using ShipEnhancements.Utils;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class CockpitGlassSelectorModule : SelectorModule
{
	[SerializeField]
	private Material[] _materials;
	[SerializeField]
	private bool[] _requiresDLC;

	private void Start()
	{
		foreach (var material in _materials)
		{
			AssetBundleUtilities.ReplaceMaterialShader(material);
		}
	}
	
	protected override OptionsListElementData[] GenerateOptionData()
	{
		List<OptionsListElementData> data = [];
		for (int i = 0; i < _optionNames.Length; i++)
		{
			if (i >= _materials.Length) break;
			if (i < _requiresDLC.Length && _requiresDLC[i] && 
				EntitlementsManager.IsDlcOwned() != EntitlementsManager.AsyncOwnershipStatus.Owned)
			{
				continue;
			}
			
			data.Add(new CockpitGlassSelectorOptionData(i, _optionNames[i], _materials[i]));
		}

		return data.ToArray();
	}

	protected override OptionsListElementData GenerateDefaultData()
	{
		return new CockpitGlassSelectorOptionData(-1, "Default", null);
	}

	protected override void OnSelectOption(OptionsListElementData data)
	{
		if (data is not CockpitGlassSelectorOptionData glassData) return;
		
		ShipDecorationManager.SetGlassMaterial(glassData._material);
	}
	
	public override float GetSelectionFadeOverride() => 0f;
}

public class CockpitGlassSelectorOptionData : OptionsListElementData
{
	public Material _material;

	public CockpitGlassSelectorOptionData(int index, string name, Material mat) : base(index, name)
	{
		_material = mat;
	}
}