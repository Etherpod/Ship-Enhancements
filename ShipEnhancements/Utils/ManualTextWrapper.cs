using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ManualTextWrapper : MonoBehaviour
{
	[SerializeField]
	private int _maxLineWidth;
	[SerializeField]
	private int _lineLimit;

	private Text _text;
	private TextStyleApplier _styleApplier;
    
	private void Awake()
	{
		_text = this.GetRequiredComponent<Text>();
		_styleApplier = GetComponent<TextStyleApplier>();
	}
    
	public void SetText(string text)
	{
		if (_text == null) return;
        
		var output = "";
		var input = text.Replace("\\n", "\n");
		float width = 0;
		float line = 0;
        
		float spacing = 0;
		if (_styleApplier != null)
		{
			spacing = _styleApplier.spacing;
		}
        
		for (int i = 0; i < input.Length; i++)
		{
			if (line >= _lineLimit) break;
            
			var add = input.Substring(i, 1);
			var ch = add.ToCharArray();
			_text.font.GetCharacterInfo(ch[0], out var info);

			if (add == "\n")
			{
				width = 0;
				line++;
                
				if (line >= _lineLimit)
				{
					break;
				}
                
				output += add;
				continue;
			}
            
			if (width + info.glyphWidth > _maxLineWidth)
			{
				width = 0;
				line++;

				if (line >= _lineLimit)
				{
					break;
				}
                
				output += "\n";
			}
            
			output += add;
			if (add == " ")
			{
				width += 20 + spacing;
			}
			else
			{
				width += info.glyphWidth + spacing;
			}
		}
        
		_text.text = output;
	}
}