using System.Linq;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class LightColorModule : ImagePreviewModule
{
	private DecoratorInterface _interface;
	
	private void Start()
	{
		_interface = FindObjectOfType<DecoratorInterface>();
	}
	
	protected override ImagePreviewElementData GenerateDefaultData()
	{
		return new LightColorElementData(-1, Color.white, Color.white);
	}

	protected override void OnSelectOption(ImagePreviewElementData data)
	{
		var lights = _interface.GetSelectionManager().GetActiveSelections()
			.SelectMany(s => s.GetComponents<ShipLightSelectionData>())
			.Where(l => l != null).ToArray();

		if (lights.Length == 0) return;
		
		if (data == _defaultData)
		{
			foreach (var light in lights)
			{
				light.ResetColor();
			}

			return;
		}
		
		if (data is LightColorElementData lightData)
		{
			foreach (var light in lights)
			{
				light.SetColor(lightData.imageColor, lightData.emissiveRendererColor);
			}
			
			return;
		}
		
		foreach (var light in lights)
		{
			light.SetColor(data.imageColor, data.imageColor);
		}
	}

	public override float GetSelectionFadeOverride() => 0.1f;
}

public class LightColorElementData : ImagePreviewElementData
{
	public Color emissiveRendererColor;

	public LightColorElementData(int index, Color lightColor, Color emissiveColor) : 
		base(index, lightColor, null)
	{
		emissiveRendererColor = emissiveColor;
	}
}