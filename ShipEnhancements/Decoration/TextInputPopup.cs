using ShipEnhancements.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.Decoration;

public class TextInputPopup : DecoratorInterfacePopup
{
	[SerializeField]
	private Text _textField;
	[SerializeField]
	private TextInputReader _inputReader;

	public override void OpenPopup(DecoratorInterfacePopupData data)
	{
		base.OpenPopup(data);
		if (data is TextInputPopupData textData)
		{
			_textField.text = textData.initialText;
		}
		_inputReader.EnableInput();
	}

	public override DecoratorInterfacePopupData ClosePopup()
	{
		_inputReader.DisableInput();
		if (_data is TextInputPopupData textData)
		{
			textData.outputText = _textField.text;
		}
		return base.ClosePopup();
	}

	public TextInputReader GetInputReader()
	{
		return _inputReader;
	}
}

public class TextInputPopupData : DecoratorInterfacePopupData
{
	public string initialText;
	public string outputText;

	public TextInputPopupData(string text)
	{
		initialText = text;
	}
}