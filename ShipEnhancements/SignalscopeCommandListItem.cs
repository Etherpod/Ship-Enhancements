using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements;

public class SignalscopeCommandListItem : MonoBehaviour
{
	[SerializeField]
	private Text _text;
	[SerializeField]
	private Color _inactiveColor = Color.gray;
	
	private ShipCommand _command;
	private bool _lastActive;

	private void Start()
	{
		_text.font = Locator.GetUIStyleManager().GetShipLogFont();
	}

	private void Update()
	{
		if (_command == null)
		{
			gameObject.SetActive(false);
			return;
		}

		bool canActivate = _command.CanActivate();
		if (_lastActive != canActivate)
		{
			_lastActive = canActivate;
			_text.color = _lastActive ? Color.white : _inactiveColor;
		}

		string displayName = _command.GetDisplayName();
		if (_text.text != displayName)
		{
			_text.text = displayName;
		}
	}

	public void DisplayCommand(ShipCommand command)
	{
		_command = command;
		_text.text = command.GetDisplayName();
		_lastActive = command.CanActivate();
		_text.color = _lastActive ? Color.white : _inactiveColor;
		gameObject.SetActive(true);
	}

	public void Clear()
	{
		_command = null;
		_text.text = string.Empty;
	}
}