using ShipEnhancements.Decoration.Modules;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorInterfaceMode : MonoBehaviour
{
	[SerializeField]
	protected string _headerOverride;
	[SerializeField]
	protected DecoratorInterfaceElement _firstSelectedElement;

	protected DecorationModule _module;
	protected DecoratorInterface _interface;

	protected virtual void Awake()
	{
		_interface = GetComponentInParent<DecoratorInterface>();
		if (_interface == null)
		{
			ShipEnhancements.WriteDebugMessage($"ERROR - No DecoratorInterface found on {gameObject.name}");
		}
	}

	public virtual void Activate()
	{
		Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.Menu_ChangeTab);
		gameObject.SetActive(true);
		if (_firstSelectedElement != null)
		{
			_firstSelectedElement.Select();
		}
	}
	
	public virtual void Deactivate()
	{
		Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.Menu_ChangeTab);
		gameObject.SetActive(false);
	}

	public void AssignModule(DecorationModule module)
	{
		_module = module;
	}

	public string GetDisplayOverride() => _headerOverride;

	public virtual float GetSelectionFadeOverride() => -1f;
}