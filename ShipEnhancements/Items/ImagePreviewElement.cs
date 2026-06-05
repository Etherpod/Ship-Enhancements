using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.Decoration;

public class ImagePreviewElement : DecoratorInterfaceElement
{
	[SerializeField]
	private Image _previewImage;

	private ImagePreviewElementData _data;

	public void Initialize(ImagePreviewElementData data)
	{
		_data = data;
		_previewImage.color = data.imageColor;
		if (data.imageTexture != null)
		{
			var mat = new Material(_previewImage.material);
			mat.mainTexture = data.imageTexture;
			_previewImage.material = mat;
		}
	}

	public ImagePreviewElementData GetData() => _data;
}

public class ImagePreviewElementData
{
	public int listIndex;
	public Color imageColor;
	public Texture2D imageTexture;

	public ImagePreviewElementData(int index, Color color, Texture2D texture = null)
	{
		listIndex = index;
		imageColor = color;
		imageTexture = texture;
	}
}