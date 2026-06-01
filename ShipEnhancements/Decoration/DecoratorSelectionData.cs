using ShipEnhancements.Decoration.Modules;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorSelectionData : MonoBehaviour
{
	[SerializeField]
	private string _displayName;
	[SerializeField]
	private DecorationModule[] _modules;

	public string GetDisplayName() => _displayName;

	public DecorationModule[] GetModules() => _modules;

	public void SetColor(Color color)
	{
		foreach (var light in SELocator.GetShipTransform().GetComponentsInChildren<ShipLight>(true))
		{
			light._light?.color = color;
			light._matPropBlock?.SetColor(light._propID_EmissionColor, color);
			light._emissiveRenderer?.SetPropertyBlock(light._matPropBlock);
		}
	}
}