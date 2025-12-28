using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Internal.Collections;
using UnityEngine;

namespace ShipEnhancements.Utils;

public static class LightmapManager
{
	private static LightmapController[] Controllers => SELocator.GetLightmapControllers();

	private static readonly ISet<Material> materials = new HashSet<Material>();

	private static void SyncMaterials()
	{
		if (Controllers.All(controller => controller._materials.Length == materials.Count)) return;
		ForAllControllers(controller => materials.UnionWith(controller._materials));
		UpdateControllers();
	}

	private static void UpdateControllers()
	{
		ForAllControllers(controller => controller._materials = materials.ToArray());
	}

	public static void AddMaterial(Material mat)
	{
		if (materials.Contains(mat))
		{
			ShipEnhancements.WriteDebugMessage("ignoring duplicate mat " + mat.name);
			return;
		}
		//ShipEnhancements.WriteDebugMessage("adding unique mat " + mat.name);
		SyncMaterials();
		materials.Add(mat);
		UpdateControllers();
	}

	public static void RemoveMaterial(Material mat)
	{
		if (!materials.Contains(mat)) return;
		SyncMaterials();
		materials.Add(mat);
		UpdateControllers();
	}

	public static void Clear() => materials.Clear();

	private static void ForAllControllers(Action<LightmapController> block)
	{
		foreach (var controller in Controllers)
		{
			block(controller);
		}
	}
}