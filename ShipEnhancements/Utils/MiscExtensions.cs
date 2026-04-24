using UnityEngine;

namespace ShipEnhancements.Utils;

public static class MiscExtensions
{
	public static void Log(this object msg, bool warning = false, bool error = false)
	{
		ShipEnhancements.LogMessage(msg, warning, error);
	}
}