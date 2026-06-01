using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class HeadlightColorModule : ColorModule
{
	private ShipHeadlightComponent _headlightComponent;
	
	private void Awake()
	{
		_headlightComponent = SELocator.GetShipTransform().GetComponentInChildren<ShipHeadlightComponent>();
	}
	
	public override void ApplyColor(Color color)
	{
		foreach (var light in _headlightComponent._headlights)
		{
			if (light._light != null)
			{
				light._light.color = color;
			}

			if (light._emissiveRenderer != null)
			{
				light._matPropBlock.SetColor(light._propID_EmissionColor, color);
				light._emissiveRenderer.SetPropertyBlock(light._matPropBlock);
			}
		}
	}
}