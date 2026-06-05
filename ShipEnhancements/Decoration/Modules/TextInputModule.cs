using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class TextInputModule : DecorationModule
{
	public override DecoratorInterfaceWindow CreateWindow(Transform parent)
	{
		var window = base.CreateWindow(parent) as TextInputWindow;
		if (window == null) return window;

		window.OnTextUpdated += OnTextUpdated;
		return window;
	}

	protected virtual void OnTextUpdated(string text) { }

	public override void DestroyWindow()
	{
		if (_currentWindow is TextInputWindow textInput)
		{
			textInput.OnTextUpdated -= OnTextUpdated;
		}
		base.DestroyWindow();
	}
}