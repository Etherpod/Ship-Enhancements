using System;
using UnityEngine;

namespace ShipEnhancements.RemoteControl;

[RequireComponent(typeof(OWCamera))]
public class ShipViewerCamera : MonoBehaviour
{
	private OWCamera _camera;
	private NoiseImageEffect _noiseEffect;
	private QuantumMoon _quantumMoon;
	private ShipComponent _component;
	
	private void Awake()
	{
		_camera = gameObject.GetRequiredComponent<OWCamera>();
		_camera.enabled = false;
		_noiseEffect = GetComponent<NoiseImageEffect>();
		GetComponent<PlanetaryFogImageEffect>().fogShader = Shader.Find("Hidden/PlanetaryFogImageEffect");
		_noiseEffect._noiseShader = Shader.Find("Hidden/NoiseImageEffect");
		_noiseEffect.strength = 1f;
		
		var rulesetDetector = SELocator.GetShipDetector().GetComponent<RulesetDetector>();
		GetComponentInChildren<FogWarpEffectBubbleController>()._rulesetDetector = rulesetDetector;
		GetComponentInChildren<CloudEffectBubbleController>()._rulesetDetector = rulesetDetector;
		GetComponentInChildren<SandEffectBubbleController>()._rulesetDetector = rulesetDetector;

		enabled = false;
	}

	private void Start()
	{
		AstroObject astroObject = Locator.GetAstroObject(AstroObject.Name.QuantumMoon);
		if (astroObject != null)
		{
			_quantumMoon = astroObject.GetComponent<QuantumMoon>();
		}
	}

	private void Update()
	{
		bool interference = HasInterference();
		if (_noiseEffect.enabled != interference)
		{
			_noiseEffect.enabled = interference;
		}
	}

	private bool HasInterference()
	{
		return (_quantumMoon != null && _quantumMoon.IsPlayerInside() != _quantumMoon.IsShipInside()) || 
			(Locator.GetCloakFieldController() != null && Locator.GetCloakFieldController().isPlayerInsideCloak != 
				Locator.GetCloakFieldController().isShipInsideCloak) || (_component != null && _component.isDamaged);
	}

	public void Enable(RenderTexture tex)
	{
		_camera.targetTexture = tex;
		_camera.enabled = true;
		_noiseEffect.enabled = HasInterference();
		enabled = true;
	}

	public void Disable()
	{
		_camera.enabled = false;
		_camera.targetTexture = null;
		enabled = false;
	}

	public void SetShipComponent(ShipComponent component)
	{
		_component = component;
	}
}