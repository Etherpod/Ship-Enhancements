using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorInterfaceMode : MonoBehaviour
{
	[SerializeField]
	private string _headerOverride;
	[SerializeField]
	private DecoratorInterfaceElement _firstSelectedElement;

	private DecoratorInterface _interface;

	private void Awake()
	{
		_interface = GetComponentInParent<DecoratorInterface>();
		if (_interface == null)
		{
			ShipEnhancements.WriteDebugMessage($"ERROR - No DecoratorInterface found on {gameObject.name}");
		}
	}

	public void Activate()
	{
		gameObject.SetActive(true);
		if (_firstSelectedElement != null)
		{
			_firstSelectedElement.Select();
		}
	}
	
	public void Deactivate()
	{
		gameObject.SetActive(false);
	}

	public string GetDisplayOverride() => _headerOverride;
}