using UnityEngine;

namespace ShipEnhancements.Decoration;

public abstract class DecoratorInterfaceElement : MonoBehaviour
{
	public delegate void SelectEvent(DecoratorInterfaceElement element);

	public event SelectEvent OnElementSelected;
	public event SelectEvent OnElementDeselected;

	[SerializeField]
	protected GameObject[] _objectsEnabledWhenSelected;
	[SerializeField]
	protected GameObject[] _objectsHiddenWhenSelected;
	[SerializeField]
	protected DecoratorInterfaceElement _selectOnLeft;
	[SerializeField]
	protected DecoratorInterfaceElement _selectOnRight;
	[SerializeField]
	protected DecoratorInterfaceElement _selectOnUp;
	[SerializeField]
	protected DecoratorInterfaceElement _selectOnDown;

	protected DecoratorInterface _interface;
	protected bool _selected;

	private void Awake()
	{
		_interface = GetComponentInParent<DecoratorInterface>();
		if (_interface == null)
		{
			ShipEnhancements.WriteDebugMessage($"ERROR - No DecoratorInterface found on {gameObject.name}");
			return;
		}
		
		_interface.AddInterfaceElement(this);
	}

	private void Start()
	{
		UpdateToggledObjects();
	}

	public void Select()
	{
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
		Submit_Internal();
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