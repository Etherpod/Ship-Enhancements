using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class HeadlightColorModule : ImagePreviewModule
{
	private ShipHeadlightComponent _headlightComponent;
	
	private void Awake()
	{
		_headlightComponent = SELocator.GetShipTransform().GetComponentInChildren<ShipHeadlightComponent>();
	}

	protected override void OnSubmitOption(ImagePreviewElementData data)
	{
		foreach (var light in _headlightComponent._headlights)
		{
			if (light._light != null)
			{
				light._light.color = data.imageColor;
			}

			if (light._emissiveRenderer != null)
			{
				light._matPropBlock.SetColor(light._propID_EmissionColor, data.imageColor);
				light._emissiveRenderer.SetPropertyBlock(light._matPropBlock);
			}
		}
	}

	public override float GetSelectionFadeOverride() => 0.1f;
}