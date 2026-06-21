using ShipEnhancements.Decoration.Modules;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorSelectionData : MonoBehaviour
{
	[SerializeField]
	private string _displayName;
	[SerializeField]
	private DecorationModule[] _modules;

	public string GetDisplayName() => _displayName;

	public DecorationModule[] GetModules() => _modules;
}