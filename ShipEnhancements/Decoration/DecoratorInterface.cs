using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.Decoration;

public class DecoratorInterface : MonoBehaviour
{
	public delegate void DeactiveInterfaceEvent();
	public event DeactiveInterfaceEvent OnInterfaceDeactivated;

	public delegate void ActivateModeEvent(float fadeOverride);
	public event ActivateModeEvent OnModeActivated;
	
	[SerializeField]
	private CanvasGroupAnimator _canvasGroupAnimator;
	[SerializeField]
	private Transform _layoutParent;
	[SerializeField]
	private Text _headerLabel;
	[SerializeField]
	private GameObject _mainOptionsObject;
	[SerializeField]
	private DecoratorInterfaceOptionsList _optionsList;

	private bool _activated;

	private List<DecoratorInterfaceElement> _elements = [];
	private DecoratorInterfaceElement _activeElement;

	private DecoratorSelectionData _selectedData;
	private List<DecoratorInterfaceMode> _modes = [];
	private DecoratorInterfaceMode _activeMode;

	private void Start()
	{
		_canvasGroupAnimator.SetImmediate(0f, new Vector3(1f, 0f, 1f));
		enabled = false;
	}

	private void Update()
	{
		if (!_activated) return;
		
		if (_activeElement != null)
		{
			UpdateNavigation();
		}
		
		if (OWInput.IsNewlyPressed(InputLibrary.cancel, InputMode.Character))
		{
			if (_activeMode != null)
			{
				SwitchToMode(null);
			}
			else
			{
				Deactivate();
			}
		}
	}

	private void UpdateNavigation()
	{
		if (OWInput.IsNewlyPressed(InputLibrary.lockOn, InputMode.Character))
		{
			ShipEnhancements.WriteDebugMessage("try submit");
			_activeElement.Submit();
			return;
		}
			
		DecoratorInterfaceElement next = null;
		if (OWInput.IsNewlyPressed(InputLibrary.toolOptionLeft, InputMode.Character))
		{
			next = _activeElement.GetElementInDirection(new Vector2(-1, 0));
		}
		else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionRight, InputMode.Character))
		{
			next = _activeElement.GetElementInDirection(new Vector2(1, 0));
		}
		else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionUp, InputMode.Character))
		{
			next = _activeElement.GetElementInDirection(new Vector2(0, 1));
		}
		else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionDown, InputMode.Character))
		{
			next = _activeElement.GetElementInDirection(new Vector2(0, -1));
		}

		if (next != null)
		{
			next.Select();
		}
	}

	public void Activate(DecoratorSelectionData data)
	{
		_selectedData = data;
		_headerLabel.text = data.GetDisplayName();

		_modes.Clear();
		_optionsList.ClearDisplayedOptions();
		if (data.GetModules().Length > 0)
		{
			_modes.AddRange(data.GetModules()
				.Select(module => module.CreateInterfaceMode(_layoutParent)));
			_optionsList.AddDisplayedOptions(_selectedData.GetModules(), _modes.ToArray());
		}
		
		Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.ShipLogSelectEntry);
		_canvasGroupAnimator.AnimateTo(1f, Vector3.one, 0.1f);
		_activated = true;
		enabled = true;
	}

	public void Deactivate()
	{
		SwitchToMode(null);
		_optionsList.ClearDisplayedOptions();
		foreach (var mode in _modes)
		{
			Destroy(mode.gameObject);
		}
		Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.ShipLogDeselectEntry);
		_canvasGroupAnimator.AnimateTo(0f, new Vector3(1f, 0f, 1f), 0.1f);
		_activated = false;
		enabled = false;
		
		OnInterfaceDeactivated?.Invoke();
	}

	private void OnElementSelected(DecoratorInterfaceElement element)
	{
		if (element == _activeElement) return;
		
		ShipEnhancements.WriteDebugMessage("switch to element " + element);

		if (_activeElement != null)
		{
			_activeElement.Deselect();
		}
		
		_activeElement = element;
	}

	public void SwitchToMode(DecoratorInterfaceMode mode)
	{
		if (mode == _activeMode)
		{
			return;
		}

		if (_activeMode != null)
		{
			_activeMode.Deactivate();
		}
		
		_activeMode = mode;

		if (mode != null)
		{
			_mainOptionsObject.SetActive(false);
			_optionsList.ClearDisplayedOptions();
			_headerLabel.text = mode.GetDisplayOverride();
			mode.Activate();
			OnModeActivated?.Invoke(mode.GetSelectionFadeOverride());
		}
		else
		{
			_headerLabel.text = _selectedData.GetDisplayName();
			_mainOptionsObject.SetActive(true);
			_optionsList.ClearDisplayedOptions();
			if (_selectedData.GetModules().Length > 0)
			{
				_optionsList.AddDisplayedOptions(_selectedData.GetModules(), _modes.ToArray());
			}
			OnModeActivated?.Invoke(-1f);
		}
	}

	public void SetDecorationColor(Color color)
	{
		if (_selectedData != null)
		{
			_selectedData.SetColor(color);
		}
	}

	public void AddInterfaceElement(DecoratorInterfaceElement element)
	{
		if (!_elements.Contains(element))
		{
			element.OnElementSelected += OnElementSelected;
			_elements.Add(element);
		}
	}

	public void RemoveInterfaceElement(DecoratorInterfaceElement element)
	{
		if (_elements.Contains(element))
		{
			element.OnElementSelected -= OnElementSelected;
			_elements.Remove(element);
		}
	}

	/*private void OnDestroy()
	{
		foreach (var element in _elements)
		{
			element.OnElementSelected -= OnElementSelected;
		}
	}*/
}