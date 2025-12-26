using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace ShipEnhancements;

[RequireComponent(typeof(Renderer))]
public class ShipTextureBlender : MonoBehaviour
{
	private static readonly int OverlayColorId = Shader.PropertyToID("_OverlayColor");
	private static readonly int BlendFactorId = Shader.PropertyToID("_BlendFactor");
	private static readonly int WoodToggleId = Shader.PropertyToID("_IsWoodTexture");

	private int materialIndex;
	private Material blendMaterial;
	private RenderTexture blendTex;
	private Color overlayColor;
	private float blendFactor;
	private Texture baseTex;
	
	private Renderer _renderer;
	
	private static readonly Dictionary<Texture, RenderTexture> RenderTextures = new();

	private void Awake()
	{
		_renderer = GetComponent<Renderer>();
	}
	
	private void Start()
	{
		if (RenderTextures.TryGetValue(baseTex, out var rt))
		{
			blendTex = rt;
		}
		else
		{
			var rendererMainTex = _renderer.materials[materialIndex].mainTexture;
			blendTex ??= new RenderTexture(rendererMainTex.width, rendererMainTex.height, 0, RenderTextureFormat.ARGBFloat);
			blendTex.Create();
			RenderTextures[baseTex] = blendTex;
		}

		_renderer.materials[materialIndex].mainTexture = blendTex;
	}

	private void OnDestroy()
	{
		RenderTextures.Remove(baseTex);
		blendTex?.Release();
	}

	private void Update()
	{
		if (OWTime.IsPaused()) return;

		if (blendMaterial is null || baseTex is null) return;

		blendMaterial.SetColor(OverlayColorId, overlayColor);
		blendMaterial.SetFloat(BlendFactorId, blendFactor);
		Graphics.Blit(baseTex, blendTex, blendMaterial);
	}

	public void Initialize(int matIndex, Material blendMat, Texture baseTexture,
		Color color, float blend = 1f, bool isWood = false)
	{
		materialIndex = matIndex;
		blendMaterial = blendMat;
		baseTex = baseTexture;
		overlayColor = color;
		blendFactor = blend;

		blendMaterial.SetFloat(WoodToggleId, isWood ? 1f : 0f);
	}

	public void SetTexture(Texture newColor, 
		Texture newNormal, Texture newSmooth,
		float normalScale, float smoothness)
	{
		baseTex = newColor;
		
		if (newNormal != null)
		{
			_renderer.materials[materialIndex].SetTexture("_BumpMap", newNormal);
		}
		
		if (newSmooth != null)
		{
			_renderer.materials[materialIndex].SetTexture("_MetallicGlossMap", newSmooth);
			_renderer.materials[materialIndex].SetFloat("_Metallic", 1f);
		}
		else if (blendMaterial.GetFloat(WoodToggleId) > 0.5f)
		{
			_renderer.materials[materialIndex].SetTexture("_MetallicGlossMap", null);
			_renderer.materials[materialIndex].SetFloat("_Metallic", 0f);
		}
        
		_renderer.materials[materialIndex].SetFloat("_BumpScale", normalScale);
		_renderer.materials[materialIndex].SetFloat("_GlossMapScale", smoothness);
	}

	public void SetColor(Color newColor)
	{
		overlayColor = newColor;
	}
}