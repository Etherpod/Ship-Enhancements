using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShipEnhancements.Decoration;

public class DecoratorInterfaceOption : DecoratorInterfaceElement
{
	private DecoratorInterfaceMode _linkedMode;
	private Text _attachedText;
	private Color _initialTextColor;
	private bool _everInitialized;

	protected override void Awake()
	{
		base.Awake();
		Initialize();
	}

	public void Initialize()
	{
		if (!_everInitialized)
		{
			_attachedText = GetComponent<Text>();
			_initialTextColor = _attachedText.color;
			_everInitialized = true;
		}
	}

	protected override void Select_Internal()
	{
		_attachedText.color = Color.white;
	}
	
	protected override void Deselect_Internal()
	{
		_attachedText.color = _initialTextColor;
	}

	protected override void Submit_Internal()
	{
		if (_linkedMode != null)
		{
			ShipEnhancements.WriteDebugMessage("Switch to mode " + _linkedMode);
			_interface.SwitchToMode(_linkedMode);
		}
	}

	public void SetDisplayText(string text)
	{
		_attachedText.text = text;
	}

	public void SetLinkedMode(DecoratorInterfaceMode mode)
	{
		_linkedMode = mode;
	}
}