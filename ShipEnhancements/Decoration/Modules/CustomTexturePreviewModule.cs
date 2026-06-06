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
	
	protected override ImagePreviewElementData[] GenerateOptionData()
	{
		List<ImagePreviewElementData> data = [];
		
		List<string> files = [];
		foreach (var type in _validFileTypes)
		{
			files.AddRange(Directory.GetFiles(Path.Combine(
					ShipEnhancements.Instance.ModHelper.Manifest.ModFolderPath, _texturePath), 
				$"*.{type}", SearchOption.TopDirectoryOnly));
		}

		for (int i = 0; i < files.Count; i++)
		{
			var fileData = File.ReadAllBytes(files[i]);
			var tex = new Texture2D(2, 2);
			tex.LoadImage(fileData);

			if (tex != null)
			{
				data.Add(new ImagePreviewElementData(i, Color.white, tex));
			}
		}
		
		return data.ToArray();
	}
}