using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class HullTexturePreset : ScriptableObject
{
	public string displayName;
	public bool dlcOnly;
	public bool hasHullTexture = true;
	public Vector2 hullTextureScale = Vector2.one;
	public List<TextureLayerElement> hullDiffuseLayers = [];
	public List<TextureLayerElement> hullSmoothnessLayers = [];
	public List<TextureLayerElement> hullNormalLayers = [];
	public float hullSmoothnessStrength = 1f;
	public float hullNormalStrength = 1f;
	public bool hasWoodTexture = true;
	public Vector2 woodTextureScale = Vector2.one;
	public List<TextureLayerElement> woodDiffuseLayers = [];
	public List<TextureLayerElement> woodSmoothnessLayers = [];
	public List<TextureLayerElement> woodNormalLayers = [];
	public float woodSmoothnessStrength = 1f;
	public float woodNormalStrength = 1f;
}

public class TextureLayerElement : ScriptableObject
{
	public Texture2D texture;
	public Vector2 scale = Vector2.one;
	public TextureBlendMode blendMode;
}

public enum TextureBlendMode
{
	None,
	Overlay,
	Average,
	Multiply,
}