using UnityEngine;

namespace ShipEnhancements;

[RequireComponent(typeof(OWCamera))]
public class ShipViewerCamera : MonoBehaviour
{
	private OWCamera _camera;
	private NoiseImageEffect _noiseEffect;
	
	private void Awake()
	{
		_camera = gameObject.GetRequiredComponent<OWCamera>();
		_camera.enabled = false;
		_noiseEffect = GetComponent<NoiseImageEffect>();
		GetComponent<PlanetaryFogImageEffect>().fogShader = Shader.Find("Hidden/PlanetaryFogImageEffect");
		_noiseEffect._noiseShader = Shader.Find("Hidden/NoiseImageEffect");
		
		var rulesetDetector = SELocator.GetShipDetector().GetComponent<RulesetDetector>();
		GetComponentInChildren<FogWarpEffectBubbleController>()._rulesetDetector = rulesetDetector;
		GetComponentInChildren<CloudEffectBubbleController>()._rulesetDetector = rulesetDetector;
		GetComponentInChildren<SandEffectBubbleController>()._rulesetDetector = rulesetDetector;
	}

	public void Enable(RenderTexture tex)
	{
		_camera.targetTexture = tex;
		_camera.enabled = true;
	}

	public void Disable()
	{
		_camera.enabled = false;
		_camera.targetTexture = null;
	}
}