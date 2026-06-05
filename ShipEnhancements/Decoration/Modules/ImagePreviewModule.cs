using System.Collections.Generic;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class ImagePreviewModule : DecorationModule
{
	[SerializeField]
	protected Color[] _colorPreviews;
	[SerializeField]
	protected Texture2D[] _texturePreviews;

	protected int _activeIndex;

	public override DecoratorInterfaceWindow CreateWindow(Transform parent)
	{
		var window = base.CreateWindow(parent) as ImagePreviewWindow;
		if (window == null) return window;

		var data = GenerateOptionData();
		window.Initialize(data);
		window.SetInitialIndex(_activeIndex);
		window.OnSelectOption += OnSelectOption;
		window.OnSubmitOption += OnSubmitOption;
		return window;
	}
	
	protected virtual ImagePreviewElementData[] GenerateOptionData()
	{
		List<ImagePreviewElementData> data = [];
		int length = Mathf.Max(_colorPreviews.Length, _texturePreviews.Length);
		for (int i = 0; i < length; i++)
		{
			Color color = Color.white;
			Texture2D tex = null;
			if (i < _colorPreviews.Length)
			{
				color = _colorPreviews[i];
			}
			if (i < _texturePreviews.Length)
			{
				tex = _texturePreviews[i];
			}
			
			data.Add(new ImagePreviewElementData(i, color, tex));
		}
		
		return data.ToArray();
	}
	
	protected virtual void OnSelectOption(ImagePreviewElementData data) { }

	protected virtual void OnSubmitOption(ImagePreviewElementData data)
	{
		_activeIndex = data.listIndex;
		if (_currentWindow is ImagePreviewWindow previewWindow)
		{
			previewWindow.SetInitialIndex(data.listIndex);
		}
	}

	public override void DestroyWindow()
	{
		base.DestroyWindow();
		if (_currentWindow is ImagePreviewWindow previewWindow)
		{
			previewWindow.OnSelectOption -= OnSelectOption;
			previewWindow.OnSubmitOption -= OnSubmitOption;
		}
	}
}