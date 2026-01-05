using UnityEngine;

namespace ShipEnhancements.Textures;

public record ShipRenderTextureInfo(
	RenderTexture Diffuse,
	RenderTexture BumpMap,
	RenderTexture GlossMap
)
{
	public bool HasGloss => GlossMap is not null;

	public void Create()
	{
		Diffuse.Create();
		BumpMap.Create();
		GlossMap?.Create();
	}

	public void Release()
	{
		Diffuse.Release();
		BumpMap.Release();
		GlossMap?.Release();
	}
}