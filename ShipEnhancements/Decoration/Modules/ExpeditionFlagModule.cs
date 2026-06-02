using ShipEnhancements.Items;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class ExpeditionFlagModule : CustomTexturePreviewModule
{
	[SerializeField]
	private ExpeditionFlagItem _flagItem;

	private void Start()
	{
		_defaultTexture = _flagItem.GetFlagTexture();
	}
	
	protected override void OnSubmitOption(ImagePreviewElementData data)
	{
		_flagItem.SetFlagTexture(data.imageTexture);
	}
	
	public override float GetSelectionFadeOverride() => 0f;
}