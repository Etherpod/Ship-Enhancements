using UnityEngine;

namespace ShipEnhancements.Decoration;

public abstract class DecoratorInterfaceElement : MonoBehaviour
{
	public delegate void ElementEvent(DecoratorInterfaceElement element);

	public event ElementEvent OnElementSelected;
	public event ElementEvent OnElementDeselected;
	public event ElementEvent OnElementSubmitted;

	[SerializeField]
	protected GameObject[] _objectsEnabledWhenSelected;
	[SerializeField]
	protected GameObject[] _objectsHiddenWhenSelected;
	[Space]
	[SerializeField]
	protected AudioType _selectAudio = AudioType.Menu_UpDown;
	[SerializeField]
	protected AudioType _submitAudio = AudioType.ShipLogMoveBetweenEntries;
	[SerializeField]
	protected AudioClip _customSelectAudio;
	[SerializeField]
	protected AudioClip _customSubmitAudio;
	[Space]
	[SerializeField]
	protected DecoratorInterfaceElement _selectOnLeft;
	[SerializeField]
	protected DecoratorInterfaceElement _selectOnRight;
	[SerializeField]
	protected DecoratorInterfaceElement _selectOnUp;
	[SerializeField]
	protected DecoratorInterfaceElement _selectOnDown;
	[Space]

	protected DecoratorInterface _interface;
	protected bool _selected;

	protected virtual void Awake()
	{
		_interface = GetComponentInParent<DecoratorInterface>();
		if (_interface == null)
		{
			ShipEnhancements.WriteDebugMessage($"ERROR - No DecoratorInterface found on {gameObject.name}");
			return;
		}
		
		_interface.AddInterfaceElement(this);
	}

	protected virtual void Start()
	{
		UpdateToggledObjects();
	}

	public void Select()
	{
		PlaySelectAudio();
		Select_Internal();
		_selected = true;
		UpdateToggledObjects();
		OnElementSelected?.Invoke(this);
	}

	protected virtual void Select_Internal() { }
	
	public void Deselect()
	{
		Deselect_Internal();
		_selected = false;
		UpdateToggledObjects();
		OnElementDeselected?.Invoke(this);
	}

	protected virtual void Deselect_Internal() { }

	public void Submit()
	{
		PlaySubmitAudio();
		Submit_Internal();
		OnElementSubmitted?.Invoke(this);
	}

	protected virtual void Submit_Internal() { }
	
	private void UpdateToggledObjects()
	{
		foreach (var obj in _objectsEnabledWhenSelected)
		{
			obj.SetActive(_selected);
		}

		foreach (var obj in _objectsHiddenWhenSelected)
		{
			obj.SetActive(!_selected);
		}
	}

	protected virtual void PlaySelectAudio()
	{
		if (_customSelectAudio != null)
		{
			Locator.GetMenuAudioController()._audioSource.PlayOneShot(_customSelectAudio);
		}
		else
		{
			Locator.GetMenuAudioController()._audioSource.PlayOneShot(_selectAudio);
		}
	}
	
	protected virtual void PlaySubmitAudio()
	{
		if (_customSubmitAudio != null)
		{
			Locator.GetMenuAudioController()._audioSource.PlayOneShot(_customSubmitAudio);
		}
		else
		{
			Locator.GetMenuAudioController()._audioSource.PlayOneShot(_submitAudio);
		}
	}

	public DecoratorInterfaceElement GetElementInDirection(Vector2 direction)
	{
		// it's either nothing or a diagonal
		// if this gets normalized before being passed in then I guess I lose
		if (direction.magnitude != 1) return null;

		return (direction.x, direction.y) switch
		{
			(-1, 0) => _selectOnLeft,
			(1, 0) => _selectOnRight,
			(0, 1) => _selectOnUp,
			(0, -1) => _selectOnDown,
			_ => null
		};
	}
	
	public void SetLeftElement(DecoratorInterfaceElement element)
	{
		_selectOnLeft = element;
	}

	public void SetRightElement(DecoratorInterfaceElement element)
	{
		_selectOnRight = element;
	}

	public void SetUpElement(DecoratorInterfaceElement element)
	{
		_selectOnUp = element;
	}
	
	public void SetDownElement(DecoratorInterfaceElement element)
	{
		_selectOnDown = element;
	}
	
	private void OnDisable()
	{
		if (_selected)
		{
			Deselect();
		}
	}

	private void OnDestroy()
	{
		if (_interface != null)
		{
			_interface.RemoveInterfaceElement(this);
		}
	}
}