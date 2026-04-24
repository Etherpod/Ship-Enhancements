using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.RemoteControl;

public class SignalscopeCommandGroupIcon : MonoBehaviour
{
	[SerializeField]
	private ShipCommand.CommandGroup _commandGroup;
	[SerializeField]
	private OWRenderer _renderer;
	[SerializeField]
	private Image _backdrop;
	[SerializeField]
	private Color _defaultColor = Color.gray;
	[SerializeField]
	private Color _highlightColor = Color.white;

	private void Start()
	{
		_renderer.SetColor(_defaultColor);
		_backdrop.enabled = false;
	}
	
	public void Select()
	{
		_renderer.SetColor(_highlightColor);
		_backdrop.enabled = true;
	}

	public void Deselect()
	{
		_renderer.SetColor(_defaultColor);
		_backdrop.enabled = false;
	}

	public ShipCommand.CommandGroup GetCommandGroup() => _commandGroup;
}