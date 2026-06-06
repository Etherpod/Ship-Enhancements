using ShipEnhancements.Items;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class ExpeditionFlagModule : CustomTexturePreviewModule
{
	[SerializeField]
	private ExpeditionFlagItem _flagItem;
	
	protected override ImagePreviewElementData GenerateDefaultData()
	{
		return new ImagePreviewElementData(-1, Color.white, _flagItem.GetFlagTexture());
	}

	protected override void OnSelectOption(ImagePreviewElementData data)
	{
		_flagItem.SetFlagTexture(data.imageTexture);
	}
	
	public override float GetSelectionFadeOverride() => 0f;
}