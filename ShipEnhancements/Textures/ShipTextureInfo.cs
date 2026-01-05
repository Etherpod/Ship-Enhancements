using System;
using ShipEnhancements.Utils;
using UnityEngine;

namespace ShipEnhancements.Textures;

[Serializable]
public record ShipTextureInfo
{
	public Texture Diffuse { get; }
	public Texture BumpMap { get; }
	public Texture GlossMap { get; }
	public bool HasGloss =>  GlossMap is not null;

	private readonly string assetRootPath;

	public ShipTextureInfo(Texture diffuse, Texture bumpMap, Texture glossMap = null)
	{
		assetRootPath = null;
		Diffuse = diffuse;
		BumpMap = bumpMap;
		GlossMap = glossMap;
	}

	public ShipTextureInfo(string assetRootPath, bool hasGlossMap = true)
	{
		this.assetRootPath = assetRootPath;
		Diffuse = LoadTexture("_d.png");
		BumpMap = LoadTexture("_n.png");
		GlossMap = hasGlossMap ? LoadTexture("_s.png") : null;
	}

	private Texture LoadTexture(string suffix)
	{
		return AssetUtils.LoadAsset<Texture>($"{assetRootPath}{suffix}");
	}
}