using UnityEngine;

namespace ShipEnhancements.Decoration;

public class DecoratorSelection : MonoBehaviour
{
	[SerializeField]
	private Collider _collider;
	[SerializeField]
	private OWRenderer _renderer;
	[SerializeField]
	private float _selectDistance = 10f;
	[SerializeField]
	private bool _interiorOnly;
	[SerializeField]
	private bool _exteriorOnly;

	private readonly int _highlightPropertyID = Shader.PropertyToID("_HighlightAmount");
	private DecoratorSelectionGroup _group;

	private bool _selected;
	private bool _highlighted;
	
	private readonly float _fadeLength = 0.3f;
	private float _fadeStartTime;
	private float _currentFade;
	private float _lastFade;
	private float _targetFade;

	private readonly float _highlightLength = 0.4f;
	private float _highlightStartTime;
	private float _currentHighlight;
	private float _lastHighlight;
	private float _targetHighlight;

	private float _fadeOverride = -1;
	
	private void Awake()
	{
		GlobalMessenger.AddListener("EnterShip", OnEnterShip);
		GlobalMessenger.AddListener("ExitShip", OnExitShip);
	}

	private void Start()
	{
		_collider.enabled = _interiorOnly == _exteriorOnly ||
			(PlayerState.IsInsideShip() && _interiorOnly) ||
			(!PlayerState.IsInsideShip() && _exteriorOnly);
		_renderer.SetFade(0);
		enabled = false;
	}

	private void Update()
	{
		var fadeLerp = Mathf.InverseLerp(_fadeStartTime, _fadeStartTime + _fadeLength, Time.time);
		_currentFade = Mathf.Lerp(_lastFade, _targetFade, fadeLerp);
		_renderer.SetFade(_currentFade);
		
		var highlightLerp = Mathf.InverseLerp(_highlightStartTime, _highlightStartTime + _highlightLength, Time.time);
		_currentHighlight = Mathf.Lerp(_lastHighlight, _targetHighlight, highlightLerp);
		_renderer.SetMaterialProperty(_highlightPropertyID, _currentHighlight);

		if (fadeLerp == 1f && highlightLerp == 1f)
		{
			enabled = false;
		}
	}

	public void SetSelected(bool selected)
	{
		if (_selected == selected) return;
		
		_selected = selected;
		_lastFade = _currentFade;
		_targetFade = selected ? 
			(_fadeOverride >= 0f ? _fadeOverride : 1f) : 
			0f;
		_fadeStartTime = Time.time;
		enabled = true;

		if (!selected)
		{
			SetActive(false);
			_fadeOverride = -1f;
		}
	}

	public void SetActive(bool active)
	{
		_highlighted = active;
		_lastHighlight = _currentHighlight;
		_targetHighlight = active ? 1f : 0f;
		_highlightStartTime = Time.time;
		enabled = true;
	}

	public void SetSelectionGroup(DecoratorSelectionGroup group)
	{
		_group = group;
	}

	public void SetFadeOverride(float fadeOverride)
	{
		_lastFade = _currentFade;
		_targetFade = fadeOverride >= 0f ? 
			fadeOverride : 
			(_selected ? 1f : 0f);
		_fadeStartTime = Time.time;
		enabled = true;
	}

	public DecoratorSelectionGroup GetSelectionGroup() => _group;

	public float GetSelectDistance() => _selectDistance;

	public bool IsSelected() => _selected;
	
	public bool IsActive() => _highlighted;

	private void OnEnterShip()
	{
		_collider.enabled = _interiorOnly || _interiorOnly == _exteriorOnly;
	}

	private void OnExitShip()
	{
		_collider.enabled = _exteriorOnly || _exteriorOnly == _interiorOnly;
	}

	private void OnDestroy()
	{
		GlobalMessenger.RemoveListener("EnterShip", OnEnterShip);
		GlobalMessenger.RemoveListener("ExitShip", OnExitShip);
	}
}