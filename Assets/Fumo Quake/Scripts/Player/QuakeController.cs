using Mono.CSharp;
using rinCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace FumoQuake
{
    public interface ITargetting
    {
        static ITargetting StaticTarget;
        public bool TargetActive { get; }
        public IEnumerable<Transform> RandomOrderedTargets { get; }
        public Vector3 FirstTargetPosition => RandomOrderedTargets.First().position;
    }
    #region Portal
    public partial class QuakeController
    {
        [SerializeField] ParticleSystem portalParticleTemplate;

        public static void ActivateEnemyNestWithPortalEffect(Transform t, float delay)
        {
            if (instance == null)
            {
                return;
            }

            instance.StartCoroutine(CO_PortalActivation(t, delay, instance.portalParticleTemplate));

            IEnumerator CO_PortalActivation(Transform nest, float delay, ParticleSystem ps)
            {
                yield return delay.WaitForSeconds();

                if (nest == null) yield break;
                for (int i = 0; i < nest.childCount; i++)
                {
                    GameObject g = nest.GetChild(i).gameObject;
                    if (g != null)
                    {
                        g.SetActive(false);
                    }
                }
                nest.gameObject.SetActive(true);

                float psDuration = 0f;

                if (ps != null)
                {
                    psDuration = ps.main.duration;
                    for (int i = 0; i < nest.childCount; i++)
                    {
                        QuakeEnemy child = nest.GetChild(i).GetComponent<QuakeEnemy>();
                        if (child == null)
                            continue;

                        var g = Instantiate(ps, Vector3.up * 0.5f + child.Center, Quaternion.identity);
                        g.Play();
                        Destroy(g.gameObject, psDuration);
                    }
                }
                if (psDuration > 0f)
                {
                    yield return psDuration.WaitForSeconds();
                }

                for (int i = 0; i < nest.childCount; i++)
                {
                    GameObject child = nest.GetChild(i).gameObject;
                    if (child == null)
                        continue;

                    child.gameObject.SetActive(true);
                }
            }
        }
    }
    #endregion
    public partial class QuakeController : MonoBehaviour, IFumoUnit, ITargetting, IQuakeHitable
    {
        static QuakeController instance;
        public Rigidbody UnitRB => quakeMover.rb;
        public GameObject unitGameObject => gameObject;
        public GameObject hitGameObject => unitGameObject;
        public float HealthWithSideEffects
        {
            get
            {
                return StoredHealth ?? 100;
            }
            set
            {
                const float MAXHEALTH = 100f;
                float delta = currentHealth;
                currentHealth = value.Clamp(0f, MAXHEALTH);
                StoredHealth = value.Clamp(0f, MAXHEALTH);
                delta = currentHealth - delta;
                if (delta >= 1f)
                {
                    QuakeTextInfoUI.AddText("You Gots " + delta.ToString("F0") + " Health");
                }

            }
        }
        float currentHealth;
        public static float? StoredHealth;
        [SerializeField] QuakeDude quakeMover;
        [SerializeField] Transform CurrentPositionNest;
        [SerializeField] List<Transform> EnemyTargets = new();
        [SerializeField] InputActionReference shootAction;
        [SerializeField] WeaponsController weaponsHandler;
        [SerializeField] LayerMask hitscan;
        private void Awake()
        {
            instance = this;
        }
        private void Start()
        {
            currentHealth = StoredHealth ?? 100f;
            if (currentHealth <= 0)
            {
                currentHealth = 100f;
            }
            StoredHealth = currentHealth;
            IsAlive = true;
            SceneLoader.WhenFinishedLoadingAdditives += FetchSpawnPoint;
        }
        private void FetchSpawnPoint()
        {
            if (QU_SpawnPoint.LoadSpawnpoint(out QU_SpawnPoint selection))
            {
                SnapTo(selection.transform);
            }
        }
        private void OnDestroy()
        {
            SceneLoader.WhenFinishedLoadingAdditives -= FetchSpawnPoint;
        }
        public IEnumerable<Transform> RandomOrderedTargets
        {
            get
            {
                foreach (Transform t in EnemyTargets.OrderByRandom())
                {
                    yield return t;
                }
            }
        }
        public bool IsAlive { get; set; }
        public Vector3 CurrentPosition
        {
            get
            {
                if (CurrentPositionNest != null)
                {
                    return CurrentPositionNest.position;
                }
                if (this == null)
                {
                    return Vector3.zero;
                }
                return transform.position;
            }
        }

        public bool TargetActive => IsAlive;

        private void OnEnable()
        {
            IFumoUnit.Player = this;
            QuakeProjectileRenderer.Observer = transform;
        }
        private void Update()
        {
            IFumoUnit.Player = this;
            IsAlive = true;
            ITargetting.StaticTarget = this;
            if (weaponsHandler is WeaponsController c && c != null)
            {
                bool shooting = shootAction.IsPressedRaw();
                if (c.CurrentWeapon != null && c.CurrentWeapon is IGunFireMode mode)
                {
                    if (mode.ClickMode == IGunFireMode.Mode.Click)
                    {
                        shooting = !shootAction.PressedLongerThan(0.03f) && shootAction.IsPressedRaw();
                    }
                }
                if (shooting)
                {
                    Ray r = weaponsHandler.IsProjectileWeapon ? quakeMover.ProjectileShootRay : quakeMover.CameraRay;
                    if (Physics.Raycast(quakeMover.CameraRay, out RaycastHit hit, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        float distance = (hit.point - r.origin).magnitude;
                        float lerp01 = distance.MapTo01(0f, 4f);
                        r = RinHelper.RayLerp(quakeMover.CameraRay, quakeMover.ProjectileShootRay, lerp01);
                    }
                    weaponsHandler.TryShootWith(r);
                }
            }
        }
        private void OnDisable()
        {
            ITargetting.StaticTarget = null;
            IsAlive = false;
        }

        Coroutine HitFlash;
        [SerializeField] Volume hitVolume;
        [SerializeField] ACWrapper hitSound;
        [SerializeField] ScenePairSO mainMenu;
        float iframeHighestDamage;
        public void Hit(IQuakeHitable.HitPacket packet)
        {
            if (packet.Damage <= 0f || (packet.Damage <= iframeHighestDamage && HitFlash != null))
                return;

            float processedDamage = 0f;
            if (HitFlash == null)
            {
                hitVolume.weight = 1f;
                HitFlash = StartCoroutine(CO_HitflashIframes(0.35f));
                iframeHighestDamage = packet.Damage;
                processedDamage = packet.Damage;
            }
            else
            {
                processedDamage = packet.Damage - iframeHighestDamage;
                iframeHighestDamage = packet.Damage;
            }
            hitSound.Play(CurrentPosition);

            currentHealth -= processedDamage;
            StoredHealth = currentHealth;

            if (currentHealth <= 0f)
            {
                SceneLoader.LoadScenePair(mainMenu, new()
                {
                    Delay = 1.25f,
                    Payload = () => GameSession.EndSession(new()
                    {
                        SubmitScore = true
                    })
                });
                IsAlive = false;
                Destroy(gameObject);
            }
            else
            {
                IsAlive = true;
            }
        }
        private IEnumerator CO_HitflashIframes(float duration)
        {
            float remaining = duration;
            while (remaining > 0f)
            {
                hitVolume.weight = remaining / duration;
                remaining -= Time.deltaTime;
                yield return null;
            }
            HitFlash = null;
            hitVolume.weight = 0f;
            iframeHighestDamage = 0f;
        }

        public void SnapTo(Vector3 v, Vector3? offset = null)
        {
            Vector3 realOffset = offset ?? new Vector3(0f, 0.25f, 0f);
            transform.position = v + realOffset;
        }
        public void SnapTo(Transform t)
        {
            Vector3 target = t.position;
            if (Physics.Raycast(new Ray(t.transform.position, Vector3.down), out RaycastHit hit, 0.75f))
            {
                target = hit.point;
            }
            SnapTo(target, Vector3.zero);
            quakeMover.MatchLook(t);
            quakeMover.ResetVelocity();
        }
    }
}
