using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ShipEnhancements.Decoration.Modules;

public class CustomTexturePreviewModule : ImagePreviewModule
{
	[SerializeField]
	protected string _texturePath;
	[SerializeField]
	protected string[] _validFileTypes;

	protected Texture2D _defaultTexture;
	
	protected override ImagePreviewElementData[] GenerateOptionData()
	{
		List<ImagePreviewElementData> data = [];
		
		if (_defaultTexture != null)
		{
			data.Add(new ImagePreviewElementData(Color.white, _defaultTexture));
		}

		List<string> files = [];
		foreach (var type in _validFileTypes)
		{
			files.AddRange(Directory.GetFiles(Path.Combine(
					ShipEnhancements.Instance.ModHelper.Manifest.ModFolderPath, _texturePath), 
				$"*.{type}", SearchOption.TopDirectoryOnly));
		}

		foreach (var file in files)
		{
			var fileData = File.ReadAllBytes(file);
			var tex = new Texture2D(2, 2);
			tex.LoadImage(fileData);

			if (tex != null)
			{
				data.Add(new ImagePreviewElementData(Color.white, tex));
			}
		}
		
		return data.ToArray();
	}
}