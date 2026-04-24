using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.RemoteControl;

public class SignalscopeCommandListItem : MonoBehaviour
{
	[SerializeField]
	private Text _text;
	[SerializeField]
	private Color _defaultColor = Color.white;
	[SerializeField]
	private Color _inactiveColor = Color.gray;
	[SerializeField]
	private Color _highlightColor = Color.cyan;
	[SerializeField]
	private GameObject _selectMarker;
	
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
			RefreshTextColor();
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
		RefreshTextColor();
		gameObject.SetActive(true);
	}

	private void RefreshTextColor()
	{
		var activeColor = _selectMarker.activeSelf ? _highlightColor : _defaultColor;
		_text.color = _lastActive ? activeColor : _inactiveColor;
	}

	public void Clear()
	{
		_command = null;
		_text.text = string.Empty;
	}

	public void Select()
	{
		_selectMarker.SetActive(true);
		RefreshTextColor();
	}

	public void Deselect()
	{
		_selectMarker.SetActive(false);
		RefreshTextColor();
	}

	public ShipCommand GetCommand() => _command;
}