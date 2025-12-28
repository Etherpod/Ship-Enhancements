using System.Collections.Generic;
using ShipEnhancements.Utils;
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
	private bool _isWood;
	
	private void Awake()
	{
		_renderer = GetComponent<Renderer>();
	}
	
	private void Start()
	{
		var (mat, rt) = this.GetBlendMaterial(_renderer.sharedMaterials[materialIndex]);
		blendTex = rt;
		
		Material[] sharedMats = _renderer.sharedMaterials;
		sharedMats[materialIndex] = mat;
		sharedMats[materialIndex].mainTexture = mat.mainTexture;
		_renderer.sharedMaterials = sharedMats;

		foreach (var sMat in _renderer.sharedMaterials)
		{
			LightmapManager.AddMaterial(sMat);
		}
    }

	private void OnDestroy()
	{
        this.FreeBlendMaterial();
	}

	private void Update()
	{
		if (OWTime.IsPaused()) return;

		if (blendMaterial is null || baseTex is null) return;

		blendMaterial.SetColor(OverlayColorId, overlayColor);
		blendMaterial.SetFloat(BlendFactorId, blendFactor);
		blendMaterial.SetFloat(WoodToggleId, _isWood ? 1f : 0f);
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
		_isWood = isWood;
	}

	public void SetTexture(Texture newColor, Texture newNormal, Texture newSmooth,
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
		ShipEnhancements.WriteDebugMessage(newColor);
		overlayColor = newColor;
	}
}