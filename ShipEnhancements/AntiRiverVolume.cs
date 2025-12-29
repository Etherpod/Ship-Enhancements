using UnityEngine;

namespace ShipEnhancements;

public class AntiRiverVolume : MonoBehaviour
{
	private Shape _shape;

	private void Awake()
	{
		_shape = GetComponent<Shape>();
	}

	public bool CheckPointInside(Vector3 worldPoint)
	{
		return _shape.PointInside(worldPoint);
	}
}