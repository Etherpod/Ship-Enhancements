using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShipEnhancements.Decoration;

public class OptionsListElement : DecoratorInterfaceElement
{
	protected OptionsListElementData _data;
	protected Text _attachedText;
	protected Color _initialTextColor;
	protected bool _everInitialized;

	protected override void Awake()
	{
		base.Awake();
		Initialize();
	}

	public void Initialize(OptionsListElementData data = null)
	{
		if (!_everInitialized)
		{
			_attachedText = GetComponent<Text>();
			_initialTextColor = _attachedText.color;
			
			if (data != null)
			{			
				_data = data;
				SetDisplayText(data.displayName);
			}
			
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

	public void SetDisplayText(string text)
	{
		_attachedText.text = text;
	}

	public OptionsListElementData GetData() => _data;
}

public class OptionsListElementData
{
	public string displayName;

	public OptionsListElementData(string name)
	{
		displayName = name;
	}
}