using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public abstract class DecorationModule : MonoBehaviour
{
	[SerializeField]
	protected string _displayName;
	[SerializeField]
	protected GameObject _interfaceWindowPrefab;

	protected DecoratorInterfaceWindow _currentWindow;

	public string GetDisplayName() => _displayName;

	public virtual DecoratorInterfaceWindow CreateWindow(Transform parent)
	{
		if (_currentWindow != null)
		{
			DestroyWindow();
		}
		
		_interfaceWindowPrefab.SetActive(false);
		_currentWindow = ShipEnhancements.CreateObject(_interfaceWindowPrefab, parent)
			.GetComponent<DecoratorInterfaceWindow>();
		_currentWindow.AssignModule(this);
		_currentWindow.SetDisplayOverride(_displayName);
		return _currentWindow;
	}

	public DecoratorInterfaceWindow GetCurrentWindow() => _currentWindow;

	public virtual float GetSelectionFadeOverride() => -1; 

	public virtual void DestroyWindow()
	{
		if (_currentWindow == null) return;

		Destroy(_currentWindow.gameObject);
		_currentWindow = null;
	}
}