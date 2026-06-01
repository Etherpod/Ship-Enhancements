using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.Utils;

public static class AssetBundleUtilities
{
    public static void ReplaceShaders(GameObject prefab)
    {
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var material in renderer.sharedMaterials)
            {
                ReplaceMaterialShader(material);
            }
        }

        foreach (var trenderer in prefab.GetComponentsInChildren<TessellatedRenderer>(true))
        {
            foreach (var material in trenderer.sharedMaterials)
            {
                ReplaceMaterialShader(material);
            }
        }

        foreach (var image in prefab.GetComponentsInChildren<Image>(true))
        {
            ReplaceMaterialShader(image.material);
        }

        foreach (var text in prefab.GetComponentsInChildren<Text>(true))
        {
            ReplaceMaterialShader(text.material);
        }
    }

    public static void ReplaceMaterialShader(Material material)
    {
        if (material == null) return;

        var replacementShader = Shader.Find(material.shader.name);
        if (replacementShader == null) return;

        // preserve override tag and render queue (for Standard shader)
        // keywords and properties are already preserved
        if (material.renderQueue != material.shader.renderQueue)
        {
            var renderType = material.GetTag("RenderType", false);
            var renderQueue = material.renderQueue;
            material.shader = replacementShader;
            material.SetOverrideTag("RenderType", renderType);
            material.renderQueue = renderQueue;
        }
        else
        {
            material.shader = replacementShader;
        }
    }
}