using System.Linq;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class HeadlightColorModule : ImagePreviewModule
{
	private ShipHeadlightComponent _headlightComponent;
	
	private void Awake()
	{
		_headlightComponent = SELocator.GetShipTransform().GetComponentInChildren<ShipHeadlightComponent>();
	}

	protected override ImagePreviewElementData GenerateDefaultData()
	{
		Color lightColor = Color.white;
		Color emissiveColor = Color.white;
		bool hasLight = false;
		bool hasEmissive = false;
		foreach (var light in _headlightComponent._headlights)
		{
			if (!hasLight && light._light != null)
			{
				lightColor = light._light.color;
				hasLight = true;
			}
			
			if (!hasEmissive && light._emissiveRenderer != null)
			{
				emissiveColor = light._matPropBlock.GetColor(light._propID_EmissionColor);
				hasEmissive = true;
			}
		}

		return new HeadlightColorElementData(-1, lightColor, emissiveColor);
	}

	protected override void OnSelectOption(ImagePreviewElementData data)
	{
		if (data is HeadlightColorElementData headlightData)
		{
			foreach (var light in _headlightComponent._headlights)
			{
				if (light._light != null)
				{
					light._light.color = headlightData.imageColor;
				}

				if (light._emissiveRenderer != null)
				{
					light._matPropBlock.SetColor(light._propID_EmissionColor, headlightData.emissiveRendererColor);
					light._emissiveRenderer.SetPropertyBlock(light._matPropBlock);
				}
			}

			return;
		}
		
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

public class HeadlightColorElementData : ImagePreviewElementData
{
	public Color emissiveRendererColor;

	public HeadlightColorElementData(int index, Color lightColor, Color emissiveColor) : 
		base(index, lightColor, null)
	{
		emissiveRendererColor = emissiveColor;
	}
}