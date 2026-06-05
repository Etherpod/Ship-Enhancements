using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class ShipLabelModule : TextInputModule
{
	[SerializeField]
	private ShipNameManager _nameManager;

	protected override void OnTextUpdated(string text)
	{
		base.OnTextUpdated(text);
		_nameManager.SetShipName(text);
	}
}