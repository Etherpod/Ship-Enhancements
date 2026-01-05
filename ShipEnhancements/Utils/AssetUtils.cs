using UnityEngine;

namespace ShipEnhancements.Utils;

public static class AssetUtils
{
	public static T LoadAsset<T>(string path) where T : Object =>
		ShipEnhancements.Instance._shipEnhancementsBundle.LoadAsset<T>(path);
}