using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.Decoration;

public class TextInputWindow : DecoratorInterfaceWindow
{
	public delegate void TextUpdateEvent(string text);
	public event TextUpdateEvent OnTextUpdated;
	
	[SerializeField]
	private Text _textPreview;
	[SerializeField]
	private Text _placeholderText;
	[SerializeField]
	private ManualTextWrapper _textWrapper;
	[SerializeField]
	private GameObject _inputPopupPrefab;

	private TextInputPopup _popup;
	private bool _popupEnabled;
	private string _rawText;

	private void Update()
	{
		if (!_popupEnabled && OWInput.IsNewlyPressed(InputLibrary.interactSecondary, InputMode.Character))
		{
			OWInput.ChangeInputMode(InputMode.Menu);
			_popup.OpenPopup(new TextInputPopupData(_rawText));
			_popup.GetInputReader().OnSubmit += OnSubmitInput;
			_popupEnabled = true;
		}
		
		_placeholderText.gameObject.SetActive(string.IsNullOrEmpty(_textPreview.text));
	}

	private void OnSubmitInput()
	{
		if (_popup == null) return;

		_popup.GetInputReader().OnSubmit -= OnSubmitInput;
		
		if (_popupEnabled)
		{
			var output = _popup.ClosePopup();
			if (output is TextInputPopupData textData)
			{
				_rawText = textData.outputText;
				_textWrapper.SetText(textData.outputText);
				OnTextUpdated?.Invoke(_rawText);
			}
			
			OWInput.ChangeInputMode(InputMode.Character);
			_popupEnabled = false;
		}
	}

	public void SetText(string text)
	{
		_rawText = text;
	}

	public override void Activate()
	{
		base.Activate();
		_textWrapper.SetText(_rawText);
		_inputPopupPrefab.SetActive(false);
		var popup = ShipEnhancements.CreateObject(_inputPopupPrefab, _interface.transform);
		_popup = popup.GetComponent<TextInputPopup>();
	}

	public override void Deactivate()
	{
		Destroy(_popup.gameObject);
		_popupEnabled = false;
		base.Deactivate();
	}
}