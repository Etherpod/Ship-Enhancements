using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class CockpitRustController : MonoBehaviour
{
    [SerializeField]
    Texture2D[] _rustTextures;

    private Material _rustMat;
    private float _rustProgression;

    private void Awake()
    {
        _rustProgression = Mathf.Lerp(1f, 0.15f, (float)rustLevel.GetValue());
    }

    private void Start()
    {
        _rustMat = GetComponentInChildren<MeshRenderer>().sharedMaterial;
        _rustMat.SetFloat("_Cutoff", _rustProgression);
        _rustMat.SetTexture("_MainTex", _rustTextures[Random.Range(0, _rustTextures.Length)]);
        _rustMat.SetTextureOffset("_MainTex", new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f)));
    }
}
