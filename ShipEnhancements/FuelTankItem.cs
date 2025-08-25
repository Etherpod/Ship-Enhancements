using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class FuelTankItem : OWItem
{
    public static readonly ItemType ItemType = ShipEnhancements.Instance.FuelTankType;

    [SerializeField]
    private InteractReceiver _interactReceiver;
    [SerializeField]
    private OWAudioSource _refuelSource;
    [SerializeField]
    private ExplosionController _explosion;
    [SerializeField]
    private AudioClip[] _groanAudio;
    [SerializeField]
    private OWAudioSource _oneShotSource;
    [SerializeField]
    private OWAudioSource _explosionSource;
    [SerializeField]
    private OWEmissiveRenderer _emissiveRenderer;

    private Collider _itemCollider;
    private ScreenPrompt _refuelPrompt;
    private ScreenPrompt _fuelDepletedPrompt;
    private float _maxFuel = 300f;
    private float _currentFuel;
    private float _refillRate = 25f;
    private bool _refillingFuel;
    private bool _focused;
    private int _numberDraining = 1;

    private float _explosionSpeed = 10f;

    public override string GetDisplayName()
    {
        return "Portable Fuel Canister";
    }

    public override void Awake()
    {
        base.Awake();
        _type = ItemType;
        _itemCollider = GetComponent<Collider>();
        _refuelPrompt = new ScreenPrompt(InputLibrary.interactSecondary, "<CMD>" + UITextLibrary.GetString(UITextType.HoldPrompt)
            + " Refill Fuel", 0, ScreenPrompt.DisplayState.Normal, false);
        _fuelDepletedPrompt = new ScreenPrompt("Fuel depleted");
        _interactReceiver.OnPressInteract += OnPressInteract;
        _interactReceiver.OnGainFocus += OnGainFocus;
        _interactReceiver.OnLoseFocus += OnLoseFocus;

        if (ShipEnhancements.ExperimentalSettings != null)
        {
            _maxFuel = Mathf.Max(ShipEnhancements.ExperimentalSettings.FuelCanister_MaxFuel, 0f);
        }
    }

    private void Start()
    {
        _interactReceiver.ChangePrompt("Pick up " + GetDisplayName());
        _fuelDepletedPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
        _currentFuel = _maxFuel;
        SetEmissiveScale(0f);

        SetupExplosion();

        List<OWCollider> colliders = [.. _colliders];
        foreach (OWCollider col in _explosion.GetComponentsInChildren<OWCollider>())
        {
            colliders.Remove(col);
        }
        _colliders = [.. colliders];
    }

    private void SetupExplosion()
    {
        float multiplier = (float)shipExplosionMultiplier.GetProperty();

        if (multiplier != 1 && multiplier > 0f)
        {
            _explosion._length *= (multiplier * 0.75f) + 0.25f;
            _explosion._forceVolume._acceleration *= (multiplier * 0.25f) + 0.75f;
            _explosion.transform.localScale *= multiplier;
            _explosion._lightRadius *= (multiplier * 0.75f) + 0.25f;
            _explosion._lightIntensity *= Mathf.Min((multiplier * 0.01f) + 0.99f, 10f);
            _explosion.GetComponent<SphereCollider>().radius = 0.1f;
            OWAudioSource audio = _explosionSource;
            audio.maxDistance *= (multiplier * 0.1f) + 0.9f;
            AnimationCurve curve = audio.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
            Keyframe[] newKeys = new Keyframe[curve.keys.Length];
            for (int i = 0; i < curve.keys.Length; i++)
            {
                newKeys[i] = curve.keys[i];
                newKeys[i].value *= (multiplier * 0.1f) + 0.9f;
            }
            AnimationCurve newCurve = new();
            foreach (Keyframe key in newKeys)
            {
                newCurve.AddKey(key);
            }
            audio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, newCurve);

            if (multiplier > 1f)
            {
                AudioLowPassFilter lowPass = audio.gameObject.AddComponent<AudioLowPassFilter>();
                lowPass.cutoffFrequency = 25000f;
                AudioReverbFilter reverb = audio.gameObject.AddComponent<AudioReverbFilter>();
                reverb.reverbPreset = AudioReverbPreset.Quarry;
                reverb.reverbPreset = AudioReverbPreset.User;
                float lerp = Mathf.InverseLerp(1f, 50f, multiplier);
                reverb.reverbLevel = Mathf.Lerp(-1000f, 400f, lerp);
                reverb.decayTime = Mathf.Lerp(1.49f, 3f, lerp);
                reverb.roomHF = -4500f;

                if (multiplier >= 10f)
                {
                    Instantiate(ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ShipExplosionExpandAudio.prefab"),
                        audio.transform).name = "ShipExplosionExpandAudio";
                }
            }
        }

        if ((bool)moreExplosionDamage.GetProperty() && multiplier > 0f)
        {
            GameObject damage = ShipEnhancements.LoadPrefab("Assets/ShipEnhancements/ExplosionDamage.prefab");
            GameObject damageObj = Instantiate(damage, _explosion.transform);
            damageObj.transform.localPosition = Vector3.zero;
            damageObj.transform.localScale = Vector3.one;
            ExplosionDamage explosionDamage = damageObj.GetComponent<ExplosionDamage>();
            explosionDamage.damageShip = true;
            explosionDamage.damageFragment = true;
            explosionDamage.unparent = true;
        }
    }

    private void Update()
    {
        _refuelPrompt.SetVisibility(false);
        _fuelDepletedPrompt.SetVisibility(false);

        if ((ShipEnhancements.ExperimentalSettings?.FuelCanister_RemoteActivation ?? false)
            && Keyboard.current.lKey.wasPressedThisFrame)
        {
            Explode();
        }
        else if (_focused)
        {
            OWItem item = Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem();
            if (item != null)
            {
                _interactReceiver.ChangePrompt("Already Holding " + item.GetDisplayName());
                _interactReceiver.SetKeyCommandVisible(false);
            }
            else
            {
                _interactReceiver.ChangePrompt("Pick Up " + GetDisplayName());
                _interactReceiver.SetKeyCommandVisible(true);
            }

            if (OWInput.IsInputMode(InputMode.Character))
            {
                if (_currentFuel <= 0f)
                {
                    _fuelDepletedPrompt.SetVisibility(true);
                }
                else if (PlayerState.IsWearingSuit())
                {
                    _refuelPrompt.SetVisibility(true);

                    if (PlayerMaxFuel())
                    {
                        _refuelPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
                    }
                    else
                    {
                        _refuelPrompt.SetDisplayState(ScreenPrompt.DisplayState.Normal);
                        if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary))
                        {
                            _refuelSource.Play();
                            ShipNotifications.PostRefuelingNotification();
                            _interactReceiver._hasInteracted = true;
                            _refillingFuel = true;

                            if (ShipEnhancements.InMultiplayer)
                            {
                                foreach (uint id in ShipEnhancements.PlayerIDs)
                                {
                                    ShipEnhancements.QSBCompat.SendToggleFuelTankDrain(id, this, true);
                                }
                            }
                        }
                        else if (OWInput.IsNewlyReleased(InputLibrary.interactSecondary))
                        {
                            StopRefuel();
                        }
                    }
                }
            }
        }

        if (_refillingFuel)
        {
            SELocator.GetPlayerResources()._currentFuel += _refillRate * Time.deltaTime;
            _currentFuel = Mathf.Max(_currentFuel - _refillRate * Time.deltaTime * _numberDraining, 0f);
            if (PlayerMaxFuel() || _currentFuel <= 0f)
            {
                StopRefuel();
            }
        }
        else if (_numberDraining > 1)
        {
            _currentFuel = Mathf.Max(_currentFuel - _refillRate * Time.deltaTime * (_numberDraining - 1), 0f);
        }
    }

    private void OnPressInteract()
    {
        ItemTool itemTool = Locator.GetToolModeSwapper().GetItemCarryTool();
        if (itemTool.GetHeldItem() == null)
        {
            itemTool.MoveItemToCarrySocket(this);
            itemTool._heldItem = this;
            Locator.GetPlayerAudioController().PlayPickUpItem(ItemType);
            Locator.GetToolModeSwapper().EquipToolMode(ToolMode.Item);
        }
    }

    private void OnGainFocus()
    {
        _focused = true;
        PatchClass.UpdateFocusedItems(true);
        Locator.GetPromptManager().AddScreenPrompt(_refuelPrompt, PromptPosition.Center, false);
        Locator.GetPromptManager().AddScreenPrompt(_fuelDepletedPrompt, PromptPosition.Center, false);
    }

    private void OnLoseFocus()
    {
        _focused = false;
        PatchClass.UpdateFocusedItems(false);
        Locator.GetPromptManager().RemoveScreenPrompt(_refuelPrompt, PromptPosition.Center);
        Locator.GetPromptManager().RemoveScreenPrompt(_fuelDepletedPrompt, PromptPosition.Center);

        if (_refillingFuel)
        {
            StopRefuel();
        }
    }

    private void StopRefuel()
    {
        _refuelSource.Stop();
        Locator.GetPlayerAudioController().PlayRefuel();
        ShipNotifications.RemoveRefuelingNotification();
        _interactReceiver._hasInteracted = false;
        _refillingFuel = false;

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendToggleFuelTankDrain(id, this, false);
            }
        }
    }

    private bool PlayerMaxFuel()
    {
        return SELocator.GetPlayerResources().GetFuelFraction() >= 1f;
    }

    public float GetFuelRatio()
    {
        return _currentFuel / _maxFuel;
    }

    public void OnImpact(ImpactData impact, Transform explosionParent = null)
    {
        if (impact.speed > _explosionSpeed && _currentFuel > 0f)
        {
            if (GetComponentInParent<ShipBody>() && (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost()))
            {
                ErnestoDetectiveController.ItWasFuelTank(impact: true);
            }

            Explode(explosionParent);
        }
    }

    public void Explode(Transform parent = null)
    {
        _explosion?.Play();
        PlayExplodeClip();
        if (parent != null)
        {
            _explosion.transform.parent = parent;
        }
        else
        {
            _explosion.transform.parent = transform.parent;
        }
        _explosion.GetComponent<SphereCollider>().enabled = true;
        if (GetComponentInParent<ShipBody>() && (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost()))
        {
            SELocator.GetShipDamageController().Explode();
        }
        else
        {
            _explosion.GetComponentInChildren<ExplosionDamage>()?.OnExplode();
        }

        if ((bool)extraNoise.GetProperty() && GetComponentInParent<ShipBody>())
        {
            SELocator.GetShipTransform().GetComponentInChildren<ShipNoiseMaker>()._noiseRadius = 800f * (float)shipExplosionMultiplier.GetProperty();
        }

        if (Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem() == this)
        {
            Locator.GetDeathManager().KillPlayer(DeathType.Default);
        }

        if (ShipEnhancements.InMultiplayer)
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendFuelTankExplosion(id, this);
            }
        }

        OnLoseFocus();
        Destroy(gameObject);
    }

    public void ExplodeRemote()
    {
        _explosion?.Play();
        PlayExplodeClip();
        _explosion.transform.parent = transform.parent;
        _explosion.GetComponent<SphereCollider>().enabled = true;
        if (GetComponentInParent<ShipBody>() && (!ShipEnhancements.InMultiplayer || ShipEnhancements.QSBAPI.GetIsHost()))
        {
            SELocator.GetShipDamageController().Explode();
        }
        else
        {
            _explosion.GetComponentInChildren<ExplosionDamage>()?.OnExplode();
        }

        if ((bool)extraNoise.GetProperty())
        {
            SELocator.GetShipTemperatureDetector().GetComponentInChildren<ShipNoiseMaker>()._noiseRadius = 800f * (float)shipExplosionMultiplier.GetProperty();
        }

        if (Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem() == this)
        {
            Locator.GetDeathManager().KillPlayer(DeathType.Default);
        }

        OnLoseFocus();
        Destroy(gameObject);
    }

    private void PlayExplodeClip()
    {
        if ((float)shipExplosionMultiplier.GetProperty() > 1f)
        {
            OWAudioSource audio = _explosionSource;
            float dist = Vector3.Distance(audio.transform.position, Locator.GetPlayerTransform().position);
            float cutoffLerp = Mathf.InverseLerp(200f, 10000f, dist);
            audio.GetComponent<AudioLowPassFilter>().cutoffFrequency = Mathf.Lerp(25000f, 300f, Mathf.Pow(cutoffLerp - 1f, 3) + 1f);

            float reverbLerp = Mathf.InverseLerp(200f, 30000f, dist);
            AudioReverbFilter reverb = audio.GetComponent<AudioReverbFilter>();
            reverb.reverbLevel += Mathf.Lerp(0f, 200f, reverbLerp);
            reverb.decayTime += Mathf.Lerp(0f, 3f, reverbLerp);

            float timeLerp = Mathf.InverseLerp(500f, 100000f, dist);
            float delay = Mathf.Lerp(0f, 6f, timeLerp);
            float delayTime = Time.time + delay;

            AudioClip clip;
            if ((float)shipExplosionMultiplier.GetProperty() >= 10f)
            {
                clip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/OW_SP_ShipExploding_LongReverb.ogg");
            }
            else
            {
                clip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/OW_SP_ShipExploding_Long.ogg");
            }

            if (delay > 0f)
            {
                ShipEnhancements.Instance.ModHelper.Events.Unity.RunWhen(() => Time.time > delayTime, () =>
                {
                    _explosionSource.PlayOneShot(clip, 1f);
                });
            }
            else
            {
                _explosionSource.PlayOneShot(clip, 1f);
            }
        }
        else
        {
            _explosionSource.PlayOneShot(AudioType.ShipDamageShipExplosion);
        }
    }

    public void PlayGroan()
    {
        if (_groanAudio.Length > 0 && _oneShotSource != null)
        {
            int i = Random.Range(0, _groanAudio.Length);
            _oneShotSource.PlayOneShot(_groanAudio[i]);
        }
    }

    public void SetEmissiveScale(float scale)
    {
        _emissiveRenderer.SetEmissiveScale(scale);
    }

    public void ToggleDrainRemote(bool draining)
    {
        _numberDraining = Mathf.Clamp(_numberDraining + (draining ? 1 : -1), 1, ShipEnhancements.QSBAPI.GetPlayerIDs().Length);

        if (ShipEnhancements.QSBAPI.GetIsHost())
        {
            foreach (uint id in ShipEnhancements.PlayerIDs)
            {
                ShipEnhancements.QSBCompat.SendFuelTankCapacity(id, this, _currentFuel);
            }
        }
    }

    public void UpdateFuelRemote(float newFuel)
    {
        _currentFuel = newFuel;
    }

    public override void DropItem(Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
    {
        base.DropItem(position, normal, parent, sector, customDropTarget);
        _itemCollider.enabled = false;
    }

    public override void PickUpItem(Transform holdTranform)
    {
        base.PickUpItem(holdTranform);
        transform.localPosition = new Vector3(0f, -0.7f, 0.2f);
    }

    public override void SocketItem(Transform socketTransform, Sector sector)
    {
        base.SocketItem(socketTransform, sector);
        transform.localPosition = Vector3.zero;
        //_itemCollider.enabled = true;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        _interactReceiver.OnPressInteract -= OnPressInteract;
        _interactReceiver.OnGainFocus -= OnGainFocus;
        _interactReceiver.OnLoseFocus -= OnLoseFocus;
    }
}
