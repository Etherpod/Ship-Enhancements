using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements;

public class UIFlashingText : MonoBehaviour
{
	private Text _text;
	private Color _initialColor;
	private Color _hiddenColor = new Color(1f, 1f, 1f, 0f);

	private void Awake()
	{
		_text = gameObject.GetRequiredComponent<Text>();
		_initialColor = _text.color;
	}

	private void Update()
	{
		bool hide = Time.timeSinceLevelLoad % 2f > 1.33f;
		_text.color = hide ? _hiddenColor : _initialColor;
	}
}