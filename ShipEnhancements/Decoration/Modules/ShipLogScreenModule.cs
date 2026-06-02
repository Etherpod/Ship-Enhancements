using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class ShipLogScreenModule : CustomTexturePreviewModule
{
	private MeshRenderer _shipLogRenderer;
	
	private void Start()
	{
		_shipLogRenderer = SELocator.GetShipBody()
			.GetComponentInChildren<ShipLogSplashScreen>()
			.GetComponent<MeshRenderer>();
		_defaultTexture = (Texture2D)_shipLogRenderer.sharedMaterial.GetTexture("_MainTex");
	}
	
	protected override void OnSubmitOption(ImagePreviewElementData data)
	{
		_shipLogRenderer.sharedMaterial.SetTexture("_MainTex", data.imageTexture);
	}

	public override float GetSelectionFadeOverride() => 0f;
}