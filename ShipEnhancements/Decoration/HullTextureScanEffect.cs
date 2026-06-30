using UnityEngine;

namespace ShipEnhancements.Decoration;

public class HullTextureScanEffect : MonoBehaviour
{
	public delegate void ScanCompleteEvent();

	public event ScanCompleteEvent OnScanComplete;
	
	private ShipTextureBlender[] _activeBlenders;
	private float _scanStartTime;
	private float _scanLength;
	private float _startFactor;
	private float _targetFactor;

	private float _idleStartTime;
	private float _idleTime;
	private bool _idling;

	private void Awake()
	{
		enabled = false;
	}

	private void Update()
	{
		if (_activeBlenders == null)
		{
			enabled = false;
			return;
		}
		
		if (_idling)
		{
			var idleT = Mathf.InverseLerp(_idleStartTime, _idleStartTime + _idleTime, Time.time);
			if (idleT == 1f)
			{
				_scanStartTime = Time.time;
				_startFactor = 0f;
				_targetFactor = 1f;
				_idling = false;
			}
			return;
		}
		
		var scanT = Mathf.InverseLerp(_scanStartTime, _scanStartTime + _scanLength, Time.time);
		foreach (var blender in _activeBlenders)
		{
			blender.ScanFactor = Mathf.Sqrt(Mathf.Lerp(_startFactor, _targetFactor, scanT));
			blender.UpdateFullTexture();
		}

		if (scanT == 1)
		{
			if (_targetFactor == 0f)
			{
				_idleStartTime = Time.time;
				_idling = true;
				OnScanComplete?.Invoke();
			}
			else
			{
				_activeBlenders = null;
				enabled = false;
			}
		}
	}
	
	public void PlayScanEffect(ShipTextureBlender[] blenders, float length, float idleTime)
	{
		_activeBlenders = blenders;
		_scanStartTime = Time.time;
		_scanLength = length;
		_startFactor = 1f;
		_targetFactor = 0f;
		_idleTime = idleTime;
		_idling = false;
		enabled = true;
	}
}