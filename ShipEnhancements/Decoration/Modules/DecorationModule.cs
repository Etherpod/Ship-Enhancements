using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public abstract class DecorationModule : MonoBehaviour
{
	[SerializeField]
	protected string _displayName;
	[SerializeField]
	protected GameObject _interfaceModePrefab;

	public string GetDisplayName() => _displayName;

	public virtual DecoratorInterfaceMode CreateInterfaceMode(Transform parent)
	{
		_interfaceModePrefab.SetActive(false);
		var mode = ShipEnhancements.CreateObject(_interfaceModePrefab, parent)
			.GetComponent<DecoratorInterfaceMode>();
		mode.AssignModule(this);
		return mode;
	}
}