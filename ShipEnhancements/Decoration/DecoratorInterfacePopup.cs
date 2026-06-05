using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorInterfacePopup : MonoBehaviour
{
	protected DecoratorInterfacePopupData _data;
	
	public virtual void OpenPopup(DecoratorInterfacePopupData data)
	{
		_data = data;
		gameObject.SetActive(true);
	}

	public virtual DecoratorInterfacePopupData ClosePopup()
	{
		gameObject.SetActive(false);
		return _data;
	}
}

public class DecoratorInterfacePopupData;