using UnityEngine;

namespace ShipEnhancements.Decoration;

public class ShipNameManager : MonoBehaviour
{
	[SerializeField]
	private ManualTextWrapper[] _labels;

	private ShipHUDMarker _shipHUDMarker;
	private MapMarker _shipMapMarker;

	private void Awake()
	{
		_shipHUDMarker = SELocator.GetShipBody().GetComponent<ShipHUDMarker>();
		_shipMapMarker = SELocator.GetShipBody().GetComponent<MapMarker>();
	}

	public void SetShipName(string shipName)
	{
		foreach (var label in _labels)
		{
			label.SetText(shipName);
		}

		shipName = shipName.Replace("\n", " ");

		_shipHUDMarker._markerLabel = shipName;
		_shipHUDMarker._canvasMarker.SetLabel(shipName);

		if (!_shipMapMarker._canvasMarkerInitialized)
		{
			_shipMapMarker.InitMarker();
		}
		_shipMapMarker._canvasMarker.SetLabel(shipName);
	}
}