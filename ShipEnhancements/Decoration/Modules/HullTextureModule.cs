using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class HullTextureModule : ImagePreviewModule
{
	[SerializeField]
	private HullTexturePreset[] _presets;
	[SerializeField]
	private int _blenderID;

	protected override ImagePreviewElementData[] GenerateOptionData()
	{
		List<HullTextureElementData> hullDatas = [];
		
		for (int i = 0; i < _presets.Length; i++)
		{
			hullDatas.Add(new HullTextureElementData(i, _presets[i]));
		}

		return hullDatas.ToArray();
	}

	protected override ImagePreviewElementData GenerateDefaultData()
	{
		return new HullTextureElementData(-1, null);
	}
	
	protected override void OnSelectOption(ImagePreviewElementData data)
	{
		if (data is not HullTextureElementData hullData) return;

		if (hullData.texturePreset == null)
		{
			ShipDecorationManager.UpdateBlenderTexture(_blenderID, null);
			return;
		}
		
		ShipDecorationManager.UpdateBlenderTexture(_blenderID, hullData.texturePreset);
	}

	public override float GetSelectionFadeOverride() => 0.1f;
}

public class HullTextureElementData : ImagePreviewElementData
{
	public HullTexturePreset texturePreset;
	
	public HullTextureElementData(int index, HullTexturePreset preset) : 
		base(index, Color.white, GetDisplayTexture(preset))
	{
		texturePreset = preset;
	}

	private static Texture2D GetDisplayTexture(HullTexturePreset preset)
	{
		if (preset == null) return null;
		
		if (preset.hasHullTexture)
		{
			return preset.hullDiffuseLayers[0].texture;
		}
		
		if (preset.hasWoodTexture)
		{
			return preset.woodDiffuseLayers[0].texture;
		}

		return null;
	}
}