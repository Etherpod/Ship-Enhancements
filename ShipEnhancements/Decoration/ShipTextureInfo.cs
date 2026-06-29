using System;
using UnityEngine;

namespace ShipEnhancements.Decoration;

[Serializable]
public record ShipTextureInfo
{
	public Texture Diffuse { get; }
	public Texture BumpMap { get; }
	public Texture GlossMap { get; }
	// store info from preset
	public float GlossStrength { get; }
	public float BumpStrength { get; }
	public float DiffuseTileFactor { get; }
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

	// load from preset
	public ShipTextureInfo(HullTexturePreset preset, bool useWood)
	{
		assetRootPath = null;
		if (useWood && preset.hasWoodTexture)
		{
			Diffuse = preset.woodDiffuseLayers[0].texture;
			BumpMap = preset.woodNormalLayers[0].texture;
			GlossMap = preset.woodSmoothnessLayers[0].texture;
			GlossStrength = preset.woodSmoothnessStrength;
			BumpStrength = preset.woodNormalStrength;
			DiffuseTileFactor = preset.woodTextureScale.x;
		}
		else if (preset.hasHullTexture)
		{
			Diffuse = preset.hullDiffuseLayers[0].texture;
			BumpMap = preset.hullNormalLayers[0].texture;
			GlossMap = preset.hullSmoothnessLayers[0].texture;
			GlossStrength = preset.hullSmoothnessStrength;
			BumpStrength = preset.hullNormalStrength;
			DiffuseTileFactor = preset.hullTextureScale.x;
		}
	}

	private Texture LoadTexture(string suffix)
	{
		return ShipEnhancements.LoadAsset<Texture>($"{assetRootPath}{suffix}");
	}
}