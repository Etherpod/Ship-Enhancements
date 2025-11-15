using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public class PatchHandler : MonoBehaviour
{
    private static PatchHandler instance;

    public static PatchHandler Instance => instance;
    public static bool EngineSputtering => Instance?._engineSputtering ?? false;
    public static bool CollidingWithShip => Instance?._collidingWithShip ?? false;
    public static bool SwapRepairPrompt => Instance?._swapRepairPrompt ?? false;
    public static UITextType FakeRepairPrompt => Instance?._fakeRepairPrompt ?? UITextType.None;

    private int _focusedItems;

    private ShipThrusterAudio _shipThrusterAudio;
    private bool _engineSputtering;
    private AudioClip _currentSputterClip;
    private Coroutine _sputterCoroutine;

    private bool _collidingWithShip;

    private RepairReceiver _lastFocusedRepairReceiver;
    private bool _swapRepairPrompt = false;
    private UITextType _fakeRepairPrompt;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        _shipThrusterAudio = SELocator.GetShipBody().GetComponentInChildren<ShipThrusterAudio>();
    }

    public void StartSputter()
    {
        AudioClip startClip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/ShipEngineSputter_Start.ogg");
        _shipThrusterAudio._ignitionSource.Stop();
        _shipThrusterAudio._isIgnitionPlaying = true;
        _shipThrusterAudio._ignitionSource.PlayOneShot(startClip);

        Instance._engineSputtering = true;
        Instance._currentSputterClip = startClip;

        if (_sputterCoroutine != null)
        {
            StopCoroutine(_sputterCoroutine);
            _sputterCoroutine = null;
        }
        _sputterCoroutine = StartCoroutine(SputterSequence());
    }

    private IEnumerator SputterSequence()
    {
        yield return new WaitForSeconds(_currentSputterClip.length - 0.005f);

        AudioClip loopClip = ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/ShipEngineSputter_Loop.ogg");
        _currentSputterClip = loopClip;

        float ratio = SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio();
        float tempLerp = Mathf.Sqrt(Mathf.InverseLerp(-0.5f, -1f, ratio));
        float difficulty = (float)temperatureDifficulty.GetProperty();

        int min = (int)Mathf.Lerp(1f, 3.1f, difficulty * tempLerp);
        int max = (int)Mathf.Lerp(3f, 7.1f, difficulty * tempLerp);

        int num = UnityEngine.Random.Range(min, max);
        for (int i = 0; i < num; i++)
        {
            _shipThrusterAudio._ignitionSource.PlayOneShot(_currentSputterClip);

            float damageChance = Mathf.Lerp(0f, 0.3f, 0.5f + (difficulty / 2f));
            if (UnityEngine.Random.value < damageChance * tempLerp)
            {
                RandomShipDamage(componentDamage: difficulty > 0.75f, damageCause: "engine_stall");
            }

            yield return new WaitForSeconds(_currentSputterClip.length - 0.005f);
        }

        _shipThrusterAudio._ignitionSource.PlayOneShot(ShipEnhancements.LoadAudio("Assets/ShipEnhancements/AudioClip/ShipEngineSputter_Complete.ogg"));

        yield return new WaitForSeconds(1f);
        _engineSputtering = false;

        _sputterCoroutine = null;
    }

    public void StopSputter()
    {
        _engineSputtering = false;
        _currentSputterClip = null;
        if (_sputterCoroutine != null)
        {
            StopCoroutine(_sputterCoroutine);
            _sputterCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public static void UpdateFocusedItems(bool focused)
    {
        if (focused)
        {
            Instance._focusedItems++;
        }
        else
        {
            Instance._focusedItems--;
        }

        FirstPersonManipulator manipulator = Locator.GetPlayerCamera().GetComponent<FirstPersonManipulator>();
        if (manipulator.GetFocusedOWItem() == null && !manipulator.HasFocusedInteractible())
        {
            Instance._focusedItems = 0;
        }
    }

    public static bool HasFocusedItem()
    {
        return Instance._focusedItems > 0;
    }

    public static Vector3 AddIgnitionSputter(ShipThrusterController __instance)
    {
        float thrustX = OWInput.GetValue(InputLibrary.thrustX, InputMode.All);
        float thrustZ = OWInput.GetValue(InputLibrary.thrustZ, InputMode.All);
        float thrustUp = OWInput.GetValue(InputLibrary.thrustUp, InputMode.All);
        float thrustDown = OWInput.GetValue(InputLibrary.thrustDown, InputMode.All);

        if (!OWInput.IsInputMode(InputMode.ShipCockpit | InputMode.LandingCam))
        {
            return Vector3.zero;
        }
        if (!__instance._shipResources.AreThrustersUsable())
        {
            return Vector3.zero;
        }
        if (__instance._autopilot.IsFlyingToDestination())
        {
            return Vector3.zero;
        }
        Vector3 fullThrust = new Vector3(thrustX, 0f, thrustZ);
        if (fullThrust.sqrMagnitude > 1f)
        {
            fullThrust.Normalize();
        }
        fullThrust.y = thrustUp - thrustDown;

        if (__instance._requireIgnition && __instance._landingManager.IsLanded())
        {
            fullThrust.x = 0f;
            fullThrust.z = 0f;
            fullThrust.y = Mathf.Clamp01(fullThrust.y);
            if (!__instance._isIgniting && __instance._lastTranslationalInput.y <= 0f && fullThrust.y > 0f)
            {
                __instance._isIgniting = true;

                float ratio = SELocator.GetShipTemperatureDetector().GetInternalTemperatureRatio();
                float tempLerp = Mathf.Sqrt(Mathf.InverseLerp(-0.5f, -1f, ratio));
                float maxChance = Mathf.Lerp(0f, 1f, Mathf.Pow((float)temperatureDifficulty.GetProperty() * 1.5f, 1/1.75f));

                float rand = (float)new System.Random().NextDouble();
                if (rand < maxChance * tempLerp)
                {
                    Instance.StartSputter();
                }
                else
                {
                    __instance._ignitionTime = Time.time;
                    GlobalMessenger.FireEvent("StartShipIgnition");
                }
            }
            if (__instance._isIgniting)
            {
                if (fullThrust.y <= 0f)
                {
                    __instance._isIgniting = false;
                    Instance.StopSputter();
                    GlobalMessenger.FireEvent("CancelShipIgnition");
                }
                else
                {
                    if (Instance._engineSputtering || Time.time < __instance._ignitionTime + __instance._ignitionDuration)
                    {
                        fullThrust.y = 0f;
                    }
                    else
                    {
                        __instance._isIgniting = false;
                        __instance._requireIgnition = false;
                        GlobalMessenger.FireEvent("CompleteShipIgnition");
                        RumbleManager.PlayShipIgnition();
                        RumbleManager.SetShipThrottleNormal();
                    }
                }
            }
        }

        float num = Mathf.Min(__instance._rulesetDetector.GetThrustLimit(), __instance._thrusterModel.GetMaxTranslationalThrust()) / __instance._thrusterModel.GetMaxTranslationalThrust();
        Vector3 vector2 = fullThrust * num;
        if (__instance._limitOrbitSpeed && __instance._shipAlignment.IsAligning() && vector2.magnitude > 0f)
        {
            Vector3 vector3 = __instance._landingRF.GetOWRigidBody().GetWorldCenterOfMass() - __instance._shipBody.GetWorldCenterOfMass();
            Vector3 vector4 = __instance._shipBody.GetVelocity() - __instance._landingRF.GetVelocity();
            Vector3 vector5 = vector4 - Vector3.Project(vector4, vector3);
            Vector3 vector6 = Quaternion.FromToRotation(-__instance._shipBody.transform.up, vector3) * __instance._shipBody.transform.TransformDirection(vector2 * __instance._thrusterModel.GetMaxTranslationalThrust());
            Vector3 vector7 = Vector3.Project(vector6, vector3);
            Vector3 vector8 = vector6 - vector7;
            Vector3 vector9 = vector5 + vector8 * Time.deltaTime;
            float magnitude = vector9.magnitude;
            float orbitSpeed = __instance._landingRF.GetOrbitSpeed(vector3.magnitude);
            if (magnitude > orbitSpeed)
            {
                vector9 = vector9.normalized * orbitSpeed;
                vector8 = (vector9 - vector5) / Time.deltaTime;
                vector6 = vector7 + vector8;
                vector2 = __instance._shipBody.transform.InverseTransformDirection(vector6 / __instance._thrusterModel.GetMaxTranslationalThrust());
                if (vector2.sqrMagnitude > 1f)
                {
                    vector2.Normalize();
                }
            }
        }
        __instance._lastTranslationalInput = fullThrust;
        return vector2;
    }

    public static void RandomShipDamage(bool componentDamage = true, bool hullDamage = true, string damageCause = "")
    {
        if (ShipEnhancements.InMultiplayer && !ShipEnhancements.QSBAPI.GetIsHost() || (float)shipDamageMultiplier.GetProperty() <= 0f) return;

        ShipComponent[] components = SELocator.GetShipDamageController()._shipComponents
            .Where((component) => component.repairFraction == 1f && !component.isDamaged).ToArray();
        ShipHull[] hulls = SELocator.GetShipDamageController()._shipHulls.Where((hull) => hull.integrity > 0f).ToArray();
        if (componentDamage && components.Length > 0 && UnityEngine.Random.value < 0.5f)
        {
            int index = UnityEngine.Random.Range(0, components.Length);
            if (!string.IsNullOrWhiteSpace(damageCause)
                && components[index] is ShipReactorComponent && !components[index].isDamaged)
            {
                ErnestoDetectiveController.SetReactorCause(damageCause);
            }
            components[index].SetDamaged(true);
        }
        else if (hulls.Length > 0)
        {
            ShipHull targetHull = hulls[UnityEngine.Random.Range(0, hulls.Length)];

            bool wasDamaged = targetHull._damaged;
            targetHull._damaged = true;
            targetHull._integrity = Mathf.Max(0f, targetHull._integrity - UnityEngine.Random.Range(0.05f, 0.15f) * (float)shipDamageMultiplier.GetProperty());
            var eventDelegate1 = (MulticastDelegate)typeof(ShipHull).GetField("OnDamaged",
                BindingFlags.Instance | BindingFlags.NonPublic
                | BindingFlags.Public).GetValue(targetHull);
            if (eventDelegate1 != null)
            {
                foreach (var handler in eventDelegate1.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, [targetHull]);
                }
            }
            if (targetHull._damageEffect != null)
            {
                targetHull._damageEffect.SetEffectBlend(1f - targetHull._integrity);
            }

            if (ShipEnhancements.InMultiplayer)
            {
                ShipEnhancements.QSBInteraction.SetHullDamaged(targetHull, !wasDamaged);
            }
        }
    }

    public static void SetPlayerStandingOnShip(bool standingOnShip)
    {
        if (Instance != null)
        {
            Instance._collidingWithShip = standingOnShip;
        }

        if (ShipEnhancements.ExperimentalSettings?.UltraQuantumShip ?? false)
        {
            SELocator.GetShipBody().GetComponentInChildren<QuantumShip>().SetPlayerStandingOnObject(standingOnShip);
        }
        else if ((bool)enableQuantumShip.GetProperty())
        {
            SELocator.GetShipBody().GetComponent<SocketedQuantumShip>().SetPlayerStandingOnObject(standingOnShip);
        }
    }

    public static void SetLastFocusedRepairReciver(RepairReceiver receiver)
    {
        if (Instance != null && receiver != Instance._lastFocusedRepairReceiver)
        {
            Instance._lastFocusedRepairReceiver = receiver;
            if (receiver != null)
            {
                Instance._swapRepairPrompt = UnityEngine.Random.value < 0.01f;

                var allTexts = Enum.GetValues(typeof(UITextType)) as UITextType[];
                var parts = allTexts.Where(text => text != receiver.GetRepairableName()
                    && text.ToString().Contains("ShipPart")).ToArray();
                Instance._fakeRepairPrompt = parts[UnityEngine.Random.Range(0, parts.Length)];
            }
        }
    }
}
