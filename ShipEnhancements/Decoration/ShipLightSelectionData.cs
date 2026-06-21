using UnityEngine;

namespace ShipEnhancements.Decoration;

public class ShipLightSelectionData : MonoBehaviour
{
	[SerializeField]
	private string _lightPath;
	[SerializeField]
	private string _emissiveRendererPath;

	private Light _light;
	private ShipLight _shipLight;
	private Renderer _renderer;
	private Color _defaultLightColor;
	private Color _defaultEmissiveColor;

	private int _propID_EmissionColor;
	private MaterialPropertyBlock _matPropBlock;

	private void Awake()
	{
		_matPropBlock = new MaterialPropertyBlock();
		_propID_EmissionColor = Shader.PropertyToID("_EmissionColor");
	}

	private void Start()
	{
		var lightObj = SELocator.GetShipTransform().Find(_lightPath);
		if (lightObj.TryGetComponent(out Light lightComponent))
		{
			_light = lightComponent;
			_defaultLightColor = lightComponent.color;
			if (lightObj.TryGetComponent(out ShipLight shipLightComponent))
			{
				_shipLight = shipLightComponent;	
			}
		}

		var rendererObj = SELocator.GetShipTransform().Find(_emissiveRendererPath);
		if (rendererObj.TryGetComponent(out Renderer rendererComponent))
		{
			_renderer = rendererComponent;
			_defaultEmissiveColor = rendererComponent.sharedMaterials[0].GetColor(_propID_EmissionColor);
		}
	}

	public void SetColor(Color lightColor, Color emissiveColor)
	{
		if (_light != null)
		{
			_light.color = lightColor;
		}

		if (_renderer != null)
		{
			if (_shipLight != null)
			{
				_shipLight._baseEmission = emissiveColor;
				_shipLight.Update();
			}
			else
			{
				_matPropBlock.SetColor(_propID_EmissionColor, emissiveColor);
				_renderer.SetPropertyBlock(_matPropBlock);
			}
		}
	}

	public void ResetColor()
	{
		SetColor(_defaultLightColor, _defaultEmissiveColor);
	}
}