using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShipEnhancements.Decoration;

public class DecoratorInterface : MonoBehaviour
{
	public delegate void DeactiveInterfaceEvent();

	public event DeactiveInterfaceEvent OnInterfaceDeactivated;
	
	[SerializeField]
	private CanvasGroupAnimator _canvasGroupAnimator;
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
	private DecoratorInterfaceMode _activeMode;

	private void Start()
	{
		_canvasGroupAnimator.SetImmediate(0f, new Vector3(1f, 0f, 1f));
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
		_optionsList.SetDisplayedOptions(_selectedData.GetOptionsToDisplay());
		
		Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.ShipLogSelectEntry);
		_canvasGroupAnimator.AnimateTo(1f, Vector3.one, 0.1f);
		_activated = true;
	}

	public void Deactivate()
	{
		SwitchToMode(null);
		_optionsList.ClearDisplayedOptions();
		Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.ShipLogDeselectEntry);
		_canvasGroupAnimator.AnimateTo(0f, new Vector3(1f, 0f, 1f), 0.1f);
		_activated = false;
		
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
		}
		else
		{
			_headerLabel.text = _selectedData.GetDisplayName();
			_mainOptionsObject.SetActive(true);
			_optionsList.SetDisplayedOptions(_selectedData.GetOptionsToDisplay());
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