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
        public Vector3 Center { get; }
        public IEnumerable<Transform> RandomOrderedTargets { get; }
        public Vector3 FirstTargetPosition => RandomOrderedTargets.First().position;
    }
    #region Object Follower
    public partial class QuakeController
    {
        [System.Serializable]
        public struct followItem
        {
            public Transform writer, reader;
        }
        [SerializeField] List<GameObject> detachedObjects = new();
        [SerializeField] List<GameObject> stateObjects = new();
        [SerializeField] List<followItem> followItems = new();
        void StateItemsFrame(bool state)
        {
            foreach (var item in stateObjects)
            {
                if (item == null)
                    continue;
                item.SetActive(state);
            }
        }
        void StartFollowItems()
        {
            foreach (var item in detachedObjects)
            {
                if (item == null)
                    continue;
                item.transform.SetParent(null);
            }
            StartCoroutine(CO_Run());
            IEnumerator CO_Run()
            {
                while (gameObject.activeInHierarchy)
                {
                    foreach (var item in followItems)
                    {
                        item.reader.SetParent(null);
                    }
                    foreach (var item in followItems)
                    {
                        if (item.writer == null || item.reader == null)
                        {
                            continue;
                        }
                        item.reader.position = item.writer.position;
                        item.reader.rotation = item.writer.rotation;
                    }
                    yield return null;
                }
            }
        }
    }
    #endregion
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
    #region Healthstate
    public partial class QuakeController : IHealthState
    {
        public float HealthState_CurrentHealth => currentHealth;
        public float HealthState_MaxHealth => MAX_HEALTH; 
    }
    #endregion
    public partial class QuakeController : MonoBehaviour, IFumoUnit, ITargetting, IQuakeHitable
    {
        static QuakeController instance;
        public Vector3 Center => quakeMover.ground.GroundedBox.bounds.center;
        public Rigidbody UnitRB => quakeMover.rb;
        public GameObject unitGameObject => gameObject;
        public GameObject hitGameObject => unitGameObject;
        public float HealthWithSideEffects
        {
            get
            {
                return StoredHealth ?? MAX_HEALTH;
            }
            set
            {
                float delta = currentHealth;
                currentHealth = value.Clamp(0f, MAX_HEALTH);
                StoredHealth = value.Clamp(0f, MAX_HEALTH);
                delta = currentHealth - delta;
                if (delta >= 1f)
                {
                    QuakeTextInfoUI.AddText("You Gots " + delta.ToString("F0") + " Health");
                }

            }
        }
        const float MAX_HEALTH = 100f;
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
            SceneLoader.WhenFinishedLoadingAdditives += FetchSpawnPoint;
            ReinitializePlayerState();
            StartFollowItems();
        }
        private void ReinitializePlayerState()
        {
            StopHitflash();
            gameObject.SetActive(true);
            currentHealth = StoredHealth ?? MAX_HEALTH;
            if (currentHealth <= 0)
            {
                currentHealth = MAX_HEALTH;
            }
            StoredHealth = currentHealth;
            IsAlive = true;
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
        float groundKillTime;
        private void Update()
        {
            if (quakeMover.ground.IsGrounded)
            {
                groundKillTime = Time.time + 5f;
            }
            else
            {
                if (Time.time >= groundKillTime &&
                    !Physics.BoxCast(Center, new(1f, 1f, 1f), Vector3.down, Quaternion.identity, 50f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    Kill();
                }
            }
            if (GeneralManager.IsPaused || SceneLoader.IsLoading)
                return;
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
                    weaponsHandler.TryShootWith(this, r);
                }
            }
        }
        private void OnEnable()
        {
            groundKillTime = Time.time + 5f;
            IFumoUnit.Player = this;
            QuakeProjectileRenderer.Observer = transform;
            StateItemsFrame(true);
        }
        private void OnDisable()
        {
            ITargetting.StaticTarget = null;
            IsAlive = false;
            StateItemsFrame(false);
        }

        Coroutine HitFlash;
        [SerializeField] Volume hitVolume;
        [SerializeField] ACWrapper hitSound;
        [SerializeField] ScenePairSO mainMenu;
        float iframeHighestDamage;
        void StopHitflash()
        {
            hitVolume.weight = 0f;
            HitFlash = null;
        }
        public void Hit(IQuakeHitable.HitPacket packet)
        {
            bool god = false;
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


            currentHealth = (currentHealth - processedDamage).Clamp(0f, MAX_HEALTH);
            StoredHealth = currentHealth;

            if (currentHealth <= 0f && !god)
            {
                Kill();
            }
            else
            {
                IsAlive = true;
            }
        }
        void Kill()
        {
            if (!IsAlive)
            {
                return;
            }
            if (!QuakeSession.CurrentAs(out QuakeSession ses))
            {
                SceneLoader.MainMenu();
            }
            ses.RestartLevel(new()
            {
                Delay = 1.75f,
                ForceReload = true,
                PostUnloadPayload = () =>
                {
                    PlayerWeaponsController.ResetWeaponState();
                }
            });
            IsAlive = false;
            gameObject.SetActive(false);
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
