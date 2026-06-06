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
	}
	
	protected override ImagePreviewElementData GenerateDefaultData()
	{
		var tex = (Texture2D)_shipLogRenderer.sharedMaterial.GetTexture("_MainTex");
		return new ImagePreviewElementData(-1, Color.white, tex);
	}
	
	protected override void OnSelectOption(ImagePreviewElementData data)
	{
		_shipLogRenderer.sharedMaterial.SetTexture("_MainTex", data.imageTexture);
	}

	public override float GetSelectionFadeOverride() => 0f;
}