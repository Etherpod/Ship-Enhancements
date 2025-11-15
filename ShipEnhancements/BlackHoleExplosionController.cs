using System.Linq;
using UnityEngine;

namespace ShipEnhancements;

public class BlackHoleExplosionController : ExplosionController
{
    [Space]
    [SerializeField] private float _targetScale;
    [SerializeField] private float _growAcceleration;
    [SerializeField] private DestructionVolume _destructionVolume;
    [SerializeField] private ForceVolume _gravityVolume;
    [SerializeField] private OWAudioSource _closeAudio;
    [SerializeField] private OWAudioSource _farAudio;
    [SerializeField] private OWAudioSource _oneShotAudio;

    private int _propID_Radius;
    private int _propID_DistortRadius;
    private SingularityController _singularity;
    private float _growSpeed;

    private new void Awake()
    {
        _propID_Radius = Shader.PropertyToID("_Radius");
        _propID_DistortRadius = Shader.PropertyToID("_MaxDistortRadius");

        _matPropBlock = new MaterialPropertyBlock();
        _matPropBlock.SetFloat(_propID_Radius, 1f);
        _matPropBlock.SetFloat(_propID_DistortRadius, 2f);

       // _renderer.enabled = false;
        _renderer.SetPropertyBlock(_matPropBlock);

        _audioController = SELocator.GetShipTransform().GetComponentInChildren<ShipAudioController>();
        _singularity = _renderer.GetComponent<SingularityController>();

        _playing = false;
        _timer = 0f;
        _growSpeed = 1f;
    }

    private new void Start()
    {
        enabled = false;
        _gravityVolume.SetVolumeActivation(false);
        _destructionVolume._collider.enabled = false;
    }

    private new void Update()
    {
        if (!_playing)
        {
            enabled = false;
            return;
        }

        float num = transform.localScale.x / _targetScale;

        if (_singularity.GetState() == SingularityController.State.Stable)
        {
            if (!_destructionVolume._collider.enabled)
            {
                _destructionVolume._collider.enabled = true;
            }

            transform.localScale += Vector3.one * Time.deltaTime * _growSpeed;

            _matPropBlock.SetFloat(_propID_Radius, transform.localScale.x * 0.8f);
            _matPropBlock.SetFloat(_propID_DistortRadius, transform.localScale.x);
            _renderer.SetPropertyBlock(_matPropBlock);

            _closeAudio.minDistance = transform.localScale.x;
            _closeAudio.maxDistance = transform.localScale.x * 5f;
            _farAudio.minDistance = transform.localScale.x;
            _farAudio.maxDistance = transform.localScale.x * 10f;

            _growSpeed += Time.deltaTime * _growAcceleration;
        }

        if (num >= 1f)
        {
            enabled = false;
        }
    }

    public float GetCurrentScale()
    {
        return transform.localScale.x;
    }

    public bool IsPlaying()
    {
        return _playing;
    }

    public void OpenBlackHole()
    {
        if (_playing) return;

        _gravityVolume.SetVolumeActivation(true);
        //_destructionVolume._collider.enabled = true;
        transform.localScale = Vector3.one * 1.25f;
        _oneShotAudio.minDistance = transform.localScale.x;
        _oneShotAudio.maxDistance = transform.localScale.x * 20f;
        _closeAudio.FadeIn(5f);
        _singularity.Create();

        if (Vector3.Distance(transform.position, Locator.GetPlayerTransform().position) < transform.localScale.x * 10f)
        {
            RumbleManager.PulseShipExplode();
        }

        _audioController.PlayShipExplodeClip();

        GameObject parent = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ReactorBlackHoleParent.prefab");
        Transform parentTransform = ShipEnhancements.CreateObject(parent).transform;
        transform.parent = parentTransform;
        ShipEnhancements.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() => parentTransform.GetComponent<OWRigidbody>().SetVelocity(Vector3.zero));

        if (!SEAchievementTracker.BlackHole && ShipEnhancements.AchievementsAPI != null)
        {
            SEAchievementTracker.BlackHole = true;
            ShipEnhancements.AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.BLACK_HOLE");
        }

        _playing = true;
        enabled = true;
    }

    public void SetInitialBlackHoleState(float scale)
    {
        _gravityVolume.SetVolumeActivation(true);
        _destructionVolume._collider.enabled = true;
        transform.localScale = Vector3.one * scale;
        _oneShotAudio.minDistance = transform.localScale.x;
        _oneShotAudio.maxDistance = transform.localScale.x * 20f;
        _closeAudio.FadeIn(5f);
        _singularity.Create();

        GameObject parent = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ReactorBlackHoleParent.prefab");
        Transform parentTransform = ShipEnhancements.CreateObject(parent).transform;
        transform.parent = parentTransform;
        ShipEnhancements.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() => parentTransform.GetComponent<OWRigidbody>().SetVelocity(Vector3.zero));

        if (!SEAchievementTracker.BlackHole && ShipEnhancements.AchievementsAPI != null)
        {
            SEAchievementTracker.BlackHole = true;
            ShipEnhancements.AchievementsAPI.EarnAchievement("SHIPENHANCEMENTS.BLACK_HOLE");
        }

        _playing = true;
        enabled = true;
    }
}
