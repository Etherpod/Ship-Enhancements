using System;
using System.Collections.Generic;
using ShipEnhancements.Textures;
using UnityEngine;

namespace ShipEnhancements.Utils;

public static class CustomMatManager
{
	private static readonly int PropIdBumpMap = Shader.PropertyToID("_BumpMap");
	private static readonly int PropIdMetallicGlossMap = Shader.PropertyToID("_MetallicGlossMap");

	private static readonly object Synchro = new();
	private static readonly Dictionary<Material, MatData> MatByBase = new();
	private static readonly Dictionary<object, MatData> MatByOwner = new();
	private static readonly Dictionary<MatData, int> MatUsers = new();

	public static Material InitializeMaterial(Material baseMaterial)
	{
		if (MatByBase.TryGetValue(baseMaterial, out var mat)) return mat.Mat;

		var baseDiffuse = baseMaterial.mainTexture;
		var baseBump = baseMaterial.GetTexture(PropIdBumpMap);
		var baseGloss = baseMaterial.GetTexture(PropIdMetallicGlossMap);
		mat = new MatData(
			baseMaterial,
			new Material(baseMaterial),
			new ShipTextureInfo(
				baseDiffuse,
				baseBump,
				baseGloss == null ? null : baseGloss
			),
			new ShipRenderTextureInfo(
				CreateRenderTexture(baseDiffuse),
				CreateRenderTexture(baseBump),
				baseGloss == null ? null : CreateRenderTexture(baseGloss)
			)
		);

		MatByBase[baseMaterial] = mat;

		mat.Tex.Create();
		mat.Mat.mainTexture = mat.Tex.Diffuse;
		mat.Mat.SetTexture(PropIdBumpMap, mat.Tex.BumpMap);
		if (mat.Tex.HasGloss) mat.Mat.SetTexture(PropIdMetallicGlossMap, mat.Tex.GlossMap);

		LightmapManager.AddMaterial(mat.Mat);

		return mat.Mat;
	}

	public static void ClearMaterials(bool clearLightmap = false)
	{
		if (clearLightmap) LightmapManager.Clear();

		MatByOwner.Clear();
		MatUsers.Clear();
		foreach (var mat in MatByBase.Values)
		{
			UnityEngine.Object.Destroy(mat.Mat);
			mat.Tex.Release();
		}

		MatByBase.Clear();
	}

	public static MatData GetCustomMaterial(
		this object owner,
		Material baseMaterial
		) =>
		Synchronized(() =>
		{
			if (MatByOwner.TryGetValue(owner, out var mat))
			{
				return mat;
			}

			if (!MatByBase.TryGetValue(baseMaterial, out mat))
			{
				throw new IndexOutOfRangeException(
					$"Custom material for [{baseMaterial.name}:{baseMaterial.GetInstanceID()}] not yet initialized.");
			}

			MatByOwner[owner] = mat;
			MatUsers[mat] = MatUsers.GetValueOrDefault(mat, 0) + 1;

			return mat;
		});

	public static void FreeCustomMaterial(this object owner) => Synchronized(() =>
	{
		if (!MatByOwner.Remove(owner, out var mat)) return;
		if (0 < --MatUsers[mat]) return;
		MatUsers.Remove(mat);
		MatByBase.Remove(mat.BaseMat);
		UnityEngine.Object.Destroy(mat.Mat);
		mat.Tex.Release();
	});

	private static RenderTexture CreateRenderTexture(Texture baseTexture) =>
		new(baseTexture.width, baseTexture.height, 0, RenderTextureFormat.ARGB32);

	private static void Synchronized(Action block)
	{
		lock (Synchro) block();
	}

	private static T Synchronized<T>(Func<T> block)
	{
		lock (Synchro) return block();
	}

	public record MatData(
		Material BaseMat,
		Material Mat,
		ShipTextureInfo BaseTex,
		ShipRenderTextureInfo Tex
	);
}