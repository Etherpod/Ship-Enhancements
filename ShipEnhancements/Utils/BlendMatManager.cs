using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements.Utils;

public static class BlendMatManager
{
    private static readonly object Synchro = new();
    private static readonly Dictionary<Texture, MatData> MatByBase = new();
    private static readonly Dictionary<object, MatData> MatByOwner = new();
    private static readonly Dictionary<MatData, int> MatUsers = new();

    public static (Material mat, RenderTexture mainTex) GetBlendMaterial(
        this object owner,
        Material baseMaterial,
        Texture baseTexture = null
    ) => Synchronized(() =>
    {
        if (MatByOwner.TryGetValue(owner, out var mat)) return (mat.Mat, mat.MainTex);

        baseTexture ??= baseMaterial.mainTexture;
        if (!MatByBase.TryGetValue(baseTexture, out mat))
        {
            mat = new MatData(
                baseTexture,
                new RenderTexture(baseTexture.width, baseTexture.height, 0, RenderTextureFormat.ARGB32),
                new Material(baseMaterial)
            );
            MatByBase[mat.BaseTex] = mat;
            mat.MainTex.Create();
            mat.Mat.mainTexture =  mat.MainTex;
        }

        MatByOwner[owner] = mat;
        MatUsers[mat] = MatUsers.GetValueOrDefault(mat, 0) + 1;

        return (mat.Mat, mat.MainTex);
    });

    public static void FreeBlendMaterial(this object owner) => Synchronized(() =>
    {
        if (!MatByOwner.Remove(owner, out var mat)) return;
        if (0 < --MatUsers[mat]) return;
        MatUsers.Remove(mat);
        MatByBase.Remove(mat.BaseTex);
        UnityEngine.Object.Destroy(mat.Mat);
        mat.MainTex.Release();
    });

    private static void Synchronized(Action block)
    {
        lock (Synchro) block();
    }

    private static T Synchronized<T>(Func<T> block)
    {
        lock (Synchro) return block();
    }

    private record MatData(
        Texture BaseTex,
        RenderTexture MainTex,
        Material Mat
    );
}