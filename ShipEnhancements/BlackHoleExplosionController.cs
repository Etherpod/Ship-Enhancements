﻿using System.Linq;
using UnityEngine;

namespace ShipEnhancements;

public class BlackHoleExplosionController : ExplosionController
{
    [Space]
    [SerializeField] private float _targetScale;
    [SerializeField] private float _growAcceleration;
    [SerializeField] private DestructionVolume _destructionVolume;
    [SerializeField] private ForceVolume _gravityVolume;

    private int _propID_Radius;
    private int _propID_DistortRadius;
    private SingularityController _singularity;
    private float _growSpeed;
    private OWAudioSource[] _audioSources;

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
        _audioSources = GetComponentsInChildren<OWAudioSource>();

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
            transform.localScale += Vector3.one * Time.deltaTime * _growSpeed;

            _matPropBlock.SetFloat(_propID_Radius, transform.localScale.x * 0.8f);
            _matPropBlock.SetFloat(_propID_DistortRadius, transform.localScale.x);
            _renderer.SetPropertyBlock(_matPropBlock);

            foreach (var source in _audioSources)
            {
                source.minDistance = transform.localScale.x;
                source.maxDistance = transform.localScale.x * 20f;
            }

            _growSpeed += Time.deltaTime * _growAcceleration;
        }

        if (num >= 1f)
        {
            _playing = false;
            enabled = false;
        }
    }

    public void OpenBlackHole()
    {
        _gravityVolume.SetVolumeActivation(true);
        _destructionVolume._collider.enabled = true;
        transform.localScale = Vector3.one * 1.25f;
        _singularity.Create();

        if (Vector3.Distance(transform.position, Locator.GetPlayerTransform().position) < transform.localScale.x)
        {
            RumbleManager.PulseShipExplode();
        }

        _audioController.PlayShipExplodeClip();

        GameObject parent = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ReactorBlackHoleParent.prefab");
        Transform parentTransform = Instantiate(parent).transform;
        transform.parent = parentTransform;
        ShipEnhancements.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() => parentTransform.GetComponent<OWRigidbody>().SetVelocity(Vector3.zero));

        _playing = true;
        enabled = true;
    }
}