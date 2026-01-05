using System;
using ShipEnhancements.Utils;
using UnityEngine;
using IDisposable = Delaunay.Utils.IDisposable;

namespace ShipEnhancements.Textures;

public class ShipTextureBlender : IDisposable
{
    private static readonly int OverlayColorId = Shader.PropertyToID("_OverlayColor");
    private static readonly int BlendFactorId = Shader.PropertyToID("_BlendFactor");
    private static readonly int DestZoneId = Shader.PropertyToID("_DestZone");
    private static readonly int DestExclusionZoneId = Shader.PropertyToID("_DestExclusionZone");
    private static readonly int SourceMapId = Shader.PropertyToID("_SourceMap");
    private static readonly int SourceMultiplierId = Shader.PropertyToID("_SourceMultiplier");
    private static readonly int BumpStrengthId = Shader.PropertyToID("_BumpScale");
    private static readonly int MetallicStrengthId = Shader.PropertyToID("_Metallic");
    private static readonly int GlossStrengthId = Shader.PropertyToID("_GlossMapScale");
    private static readonly Color DefaultColor = new Color(1, 1, 1);

    public Vector4 DestZone { get; set; }
    public Vector4 DestExclusionZone { get; set; }
    public Material BaseMaterial { get; }
    public Material BlendedMaterial { get; }
    public Color OverlayColor { get; set; }
    public float BlendFactor { get; set; }

    public float BumpStrength
    {
        get => BlendedMaterial.GetFloat(BumpStrengthId);
        set => BlendedMaterial.SetFloat(BumpStrengthId, value);
    }

    public float MetallicStrength
    {
        get => BlendedMaterial.GetFloat(MetallicStrengthId);
        set => BlendedMaterial.SetFloat(MetallicStrengthId, value);
    }

    public float GlossStrength
    {
        get => BlendedMaterial.GetFloat(GlossStrengthId);
        set => BlendedMaterial.SetFloat(GlossStrengthId, value);
    }

    public float BumpMultiplier { get; set; } = 1f;
    public float GlossMultiplier { get; set; } = 1f;
    public ShipTextureInfo BaseTexture { get; set; }
    public ShipTextureInfo SourceTexture { get; set; }

    private readonly Material blendingMaterial;
    private readonly ShipRenderTextureInfo targetTex;

    public ShipTextureBlender(
        Material blendingMaterial,
        Material baseMaterial,
        Vector4 destZone,
        Vector4? destExclusionZone = null,
        Color? overlayColor = null,
        float initialBlendFactor = 0f,
        float initialBumpStrength = 1f,
        float initialMetallicStrength = 0f,
        float initialGlossStrength = 0f
    )
    {
        if (blendingMaterial == null)
            throw new NullReferenceException($"{nameof(blendingMaterial)} is null but must not be.");
        this.blendingMaterial = blendingMaterial;
        DestZone = destZone;
        DestExclusionZone = destExclusionZone ?? Vector4.zero;

        BaseMaterial = baseMaterial;
        OverlayColor = overlayColor ?? DefaultColor;
        BlendFactor = initialBlendFactor;

        var mat = this.GetCustomMaterial(baseMaterial);
        BlendedMaterial = mat.Mat;
        BaseTexture = mat.BaseTex;
        targetTex = mat.Tex;
        
        BumpStrength = initialBumpStrength;
        MetallicStrength = initialMetallicStrength;
        GlossStrength = initialGlossStrength;
    }

    public void Dispose()
    {
        this.FreeCustomMaterial();
    }

    public bool UpdateBlend()
    {
        // $"[Q6J] update blend | {BlendedMaterial.name}".Log();

        // if (OWTime.IsPaused()) return false;

        if (blendingMaterial is null || SourceTexture is null)
        {
            $"[Q6J] can't update blend | {BlendedMaterial.name}".Log();
            return false;
        }

        // $"[Q6J] update blend [2] | {BlendedMaterial.name}".Log();

        blendingMaterial.SetTexture(SourceMapId, SourceTexture.Diffuse);
        blendingMaterial.SetVector(DestZoneId, DestZone);
        blendingMaterial.SetVector(DestExclusionZoneId, DestExclusionZone);
        blendingMaterial.SetColor(OverlayColorId, OverlayColor);
        blendingMaterial.SetFloat(BlendFactorId, BlendFactor);
        Graphics.Blit(BaseTexture.Diffuse, targetTex.Diffuse, blendingMaterial, 0);

        return true;
    }

    public void UpdateFullTexture()
    {
        // $"[Q6J] update blend full | {BlendedMaterial.name}".Log();

        if (!UpdateBlend()) return;
        
        // $"[Q6J] update blend full [2] | {BlendedMaterial.name}".Log();
        
        blendingMaterial.SetTexture(SourceMapId, SourceTexture.BumpMap);
        blendingMaterial.SetFloat(SourceMultiplierId, BumpMultiplier);
        Graphics.Blit(BaseTexture.BumpMap, targetTex.BumpMap, blendingMaterial, 1);

        if (!SourceTexture.HasGloss) return;
        blendingMaterial.SetTexture(SourceMapId, SourceTexture.GlossMap);
        blendingMaterial.SetFloat(SourceMultiplierId, GlossMultiplier);
        Graphics.Blit(BaseTexture.GlossMap, targetTex.GlossMap, blendingMaterial, 2);
    }
}