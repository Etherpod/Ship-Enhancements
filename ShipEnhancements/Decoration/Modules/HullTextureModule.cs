using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class HullTextureModule : ImagePreviewModule
{
	[SerializeField]
	private string[] _texturePaths;
	[SerializeField]
	private int _blenderID;

	protected override ImagePreviewElementData[] GenerateOptionData()
	{
		List<HullTextureElementData> hullDatas = [];
		
		for (int i = 0; i < _texturePaths.Length; i++)
		{
			hullDatas.Add(new HullTextureElementData(i, _texturePaths[i]));
		}

		return hullDatas.ToArray();
	}

	protected override ImagePreviewElementData GenerateDefaultData()
	{
		return new HullTextureElementData(-1, "Default");
	}
	
	protected override void OnSelectOption(ImagePreviewElementData data)
	{
		if (data is not HullTextureElementData hullData) return;

		if (hullData.texturePath == "Default")
		{
			ShipDecorationManager.UpdateBlenderTexture(_blenderID, hullData.texturePath);
			return;
		}
		
		ShipDecorationManager.UpdateBlenderTexture(_blenderID, 
			"Assets/ShipEnhancements/Decoration/ShipTextures/WoodTextures/SE_Wood_" + 
			hullData.texturePath);
	}

	public override float GetSelectionFadeOverride() => 0.1f;
}

public class HullTextureElementData : ImagePreviewElementData
{
	public string texturePath;
	//public Texture2D smoothTexture;
	//public Texture2D normalTexture;
	
	public HullTextureElementData(int index, string texPath) : 
		base(index, Color.white, GetColorTexture(texPath))
	{
		texturePath = texPath;
		//smoothTexture = ShipEnhancements.LoadAsset<Texture2D>(texPath + "_s.png");
		//normalTexture = ShipEnhancements.LoadAsset<Texture2D>(texPath + "_n.png");
	}

	private static Texture2D GetColorTexture(string path)
	{
		if (path == "Default") return null;
		
		return ShipEnhancements.LoadAsset<Texture2D>(
			"Assets/ShipEnhancements/Decoration/ShipTextures/WoodTextures/SE_Wood_" + 
			path + "_d.png");
	}
}