using ShipEnhancements.Decoration.Modules;
using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorInterfaceWindow : MonoBehaviour
{
	[SerializeField]
	protected string _headerOverride;
	[SerializeField]
	protected DecoratorInterfaceElement _firstSelectedElement;
	[SerializeField]
	private bool _useTabAudio = true;

	protected DecorationModule _module;
	protected DecoratorInterface _interface;
	private bool _active;

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
		if (_active) return;

		if (_useTabAudio)
		{
			PlayAudio();
		}
		gameObject.SetActive(true);
		if (_firstSelectedElement != null)
		{
			_firstSelectedElement.Select();
		}

		_active = true;
	}
	
	public virtual void Deactivate()
	{
		if (!_active) return;

		if (_useTabAudio)
		{
			PlayAudio();
		}
		gameObject.SetActive(false);
		_active = false;
	}

	private void PlayAudio()
	{
		Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.Menu_ChangeTab);
	}

	public void AssignModule(DecorationModule module)
	{
		_module = module;
	}

	public void SetDisplayOverride(string text) => _headerOverride = text;

	public string GetDisplayOverride() => _headerOverride;

	public float GetSelectionFadeOverride()
	{
		if (_module == null) return -1;

		return _module.GetSelectionFadeOverride();
	}
}