using rinCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FumoQuake
{
    public interface ITargetting
    {
        static ITargetting StaticTarget;
        public bool TargetActive { get; }
        public IEnumerable<Transform> RandomOrderedTargets { get; }
    }
    public class QuakeController : MonoBehaviour, IFumoUnit, ITargetting, IQuakeHitable
    {
        public GameObject unitGameObject => gameObject;
        public GameObject hitGameObject => unitGameObject;
        float currentHealth = 10f;
        [SerializeField] QuakeDude quakeMover;
        [SerializeField] Transform CurrentPositionNest;
        [SerializeField] List<Transform> EnemyTargets = new();
        [SerializeField] InputActionReference shootAction;
        [SerializeField] WeaponsController weaponsHandler;
        [SerializeField] LayerMask hitscan;
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
            if (shootAction.IsPressedRaw() && weaponsHandler != null)
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
        private void OnDisable()
        {
            ITargetting.StaticTarget = null;
            IsAlive = false;
        }

        public void Hit(IQuakeHitable.HitPacket packet)
        {
            bool AliveCheck()
            {
                if (currentHealth <= 0f)
                {
                    IsAlive = false;
                    return false;
                }
                IsAlive = true;
                return true;
            }
            currentHealth -= packet.Damage;
            if (!AliveCheck())
            {
                Destroy(gameObject);
            }
        }
    }
}
