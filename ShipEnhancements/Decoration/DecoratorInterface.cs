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
	private OptionsListWindow _defaultOptions;

	private bool _activated;

	private List<DecoratorInterfaceElement> _elements = [];
	private DecoratorInterfaceElement _selectedElement;

	private DecoratorSelectionData _selectedData;
	private DecoratorInterfaceWindow _activeWindow;

	private void Start()
	{
		_canvasGroupAnimator.SetImmediate(0f, new Vector3(1f, 0f, 1f));
		enabled = false;
	}

	private void Update()
	{
		if (!_activated) return;
		
		if (_selectedElement != null && _selectedElement.gameObject.activeInHierarchy)
		{
			UpdateNavigation();
		}
		
		if (OWInput.IsNewlyPressed(InputLibrary.cancel, InputMode.Character))
		{
			if (_activeWindow != _defaultOptions)
			{
				SwitchToWindow(_defaultOptions);
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
			_selectedElement.Submit();
			return;
		}
			
		DecoratorInterfaceElement next = null;
		if (OWInput.IsNewlyPressed(InputLibrary.toolOptionLeft, InputMode.Character))
		{
			next = _selectedElement.GetElementInDirection(new Vector2(-1, 0));
		}
		else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionRight, InputMode.Character))
		{
			next = _selectedElement.GetElementInDirection(new Vector2(1, 0));
		}
		else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionUp, InputMode.Character))
		{
			next = _selectedElement.GetElementInDirection(new Vector2(0, 1));
		}
		else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionDown, InputMode.Character))
		{
			next = _selectedElement.GetElementInDirection(new Vector2(0, -1));
		}

		if (next != null)
		{
			next.Select();
		}
	}

	public void Activate(DecoratorSelectionData data)
	{
		if (_activated) return;
		
		_selectedData = data;
		_defaultOptions.SetDisplayOverride(data.GetDisplayName());
		
		if (data.GetModules().Length > 0)
		{
			List<OptionsListElementData> windowData = [];
			for (int i = 0; i < data.GetModules().Length; i++)
			{
				if (data.GetModules()[i] == null)
				{
					ShipEnhancements.WriteDebugMessage("ERROR - Null module on " + data.gameObject.name);
					continue;
				}
				var window = data.GetModules()[i].CreateWindow(_layoutParent);
				windowData.Add(new WindowOptionData(i, data.GetModules()[i].GetDisplayName(), window));
			}
			
			_defaultOptions.AddDisplayedOptions(windowData.ToArray());
			_defaultOptions.OnSubmitOption += OnSubmitWindowOption;
		}
		
		SwitchToWindow(_defaultOptions);
		
		Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.ShipLogSelectEntry);
		_canvasGroupAnimator.AnimateTo(1f, Vector3.one, 0.1f);
		_activated = true;
		enabled = true;
	}

	public void Deactivate()
	{
		if (!_activated) return;
		
		if (_activeWindow != null)
		{
			_activeWindow.Deactivate();
			_activeWindow = null;
		}
		
		_defaultOptions.ClearDisplayedOptions();
		_defaultOptions.OnSubmitOption -= OnSubmitWindowOption;
		
		foreach (var module in _selectedData.GetModules())
		{
			module.DestroyWindow();
		}
		
		Locator.GetMenuAudioController()._audioSource.PlayOneShot(AudioType.ShipLogDeselectEntry);
		_canvasGroupAnimator.AnimateTo(0f, new Vector3(1f, 0f, 1f), 0.1f);
		_activated = false;
		enabled = false;
		
		OnInterfaceDeactivated?.Invoke();
	}

	private void OnElementSelected(DecoratorInterfaceElement element)
	{
		if (element == _selectedElement) return;

		if (_selectedElement != null)
		{
			_selectedElement.Deselect();
		}
		
		_selectedElement = element;
	}

	public DecoratorInterfaceElement GetSelectedElement() => _selectedElement;

	private void OnSubmitWindowOption(OptionsListElementData data)
	{
		if (data is not WindowOptionData windowData) return;
		
		SwitchToWindow(windowData.linkedWindow);
	}

	public void SwitchToWindow(DecoratorInterfaceWindow window)
	{
		if (window == _activeWindow)
		{
			return;
		}

		if (_activeWindow != null)
		{
			_activeWindow.Deactivate();
		}
		
		_activeWindow = window;
		
		_headerLabel.text = window.GetDisplayOverride();
		window.Activate();
		OnModeActivated?.Invoke(window.GetSelectionFadeOverride());
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
}

public class WindowOptionData : OptionsListElementData
{
	public DecoratorInterfaceWindow linkedWindow;

	public WindowOptionData(int index, string name, DecoratorInterfaceWindow window) : base(index, name)
	{
		linkedWindow = window;
	}
}