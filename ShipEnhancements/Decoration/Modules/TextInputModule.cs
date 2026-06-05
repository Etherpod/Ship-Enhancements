using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class TextInputModule : DecorationModule
{
	protected string _savedText = "";
	
	public override DecoratorInterfaceWindow CreateWindow(Transform parent)
	{
		var window = base.CreateWindow(parent) as TextInputWindow;
		if (window == null) return window;

		window.SetText(_savedText);
		window.OnTextUpdated += OnTextUpdated;
		return window;
	}

	protected virtual void OnTextUpdated(string text)
	{
		_savedText = text;
	}

	public override void DestroyWindow()
	{
		if (_currentWindow is TextInputWindow textInput)
		{
			textInput.OnTextUpdated -= OnTextUpdated;
		}
		base.DestroyWindow();
	}
}