using UnityEngine;

namespace ShipEnhancements;

public static class AssetBundleUtilities
{
    public static void ReplaceShaders(GameObject prefab)
    {
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var material in renderer.sharedMaterials)
            {
                if (material == null) continue;

                var replacementShader = Shader.Find(material.shader.name);
                if (replacementShader == null) continue;

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

        foreach (var trenderer in prefab.GetComponentsInChildren<TessellatedRenderer>(true))
        {
            foreach (var material in trenderer.sharedMaterials)
            {
                if (material == null) continue;

                var replacementShader = Shader.Find(material.shader.name);
                if (replacementShader == null) continue;

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
    }
}