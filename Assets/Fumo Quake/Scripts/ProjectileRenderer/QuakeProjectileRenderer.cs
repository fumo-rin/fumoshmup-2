using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoQuake
{
    public enum QuakeFaction
    {
        Default = 0,
        Player = 5,
        Enemy = 1,
    }
    public class QuakeProjectile
    {
        public enum HitActionResult
        {
            None = 0,
            Terrain = 10,
            IHit = 20,
        }
        public Action<HitActionResult> HitAction;
        public float SpawnTime;
        public float GravityMod = 0f;
        public float maxGravityRampingDuration = 3f;
        public float Damage = 10f;
        public int Channel = 0;
        public QuakeFaction Faction = QuakeFaction.Default;
        public bool QueueRemoval;
        public Vector3 _currentPosition;
        public Vector3 _positionOffset;
        public Vector3 _projectileVelocity;
        public ITargetting Sender;
        public Vector3 FinalizedPosition => _currentPosition + _positionOffset;
        private Vector3 GravityVelocity => (GravityMod != 0f ? (Vector3.down.ScaleToMagnitude(9.81f * GravityMod.Multiply(Time.time - SpawnTime).Clamp(0f, maxGravityRampingDuration))) : Vector3.zero);
        public Vector3 EffectiveVelocity => _projectileVelocity + GravityVelocity;
        public float Speed => EffectiveVelocity.magnitude;
        public static HashSet<QuakeProjectile> projectileQueue;
        public static void ScheduleClear(QuakeProjectile p)
        {
            if (p == null)
            {
                return;
            }
            p.QueueRemoval = true;
        }
        public IQuakeHitable.HitPacket HitPacket => new()
        {
            Damage = Damage,
            HitPoint = FinalizedPosition,
            Sender = Sender
        };
        public struct SingleProjectilePacket
        {
            public Vector3 Origin;
            public Vector3 Direction;
            public float Speed;
            public QuakeFaction Faction;
        }
        public static void CreateProjectile(SingleProjectilePacket packet, out QuakeProjectile p)
        {
            p = new QuakeProjectile()
            {
                HitAction = null,
                SpawnTime = Time.time,
                maxGravityRampingDuration = 3f,
                Channel = 0,
                GravityMod = 0f,
                Damage = 10f,
                Faction = packet.Faction,
                QueueRemoval = false,
                _currentPosition = packet.Origin,
                _projectileVelocity = packet.Direction.normalized.ScaleToMagnitude(packet.Speed)
            };
            if (QuakeProjectile.projectileQueue == null)
                QuakeProjectile.projectileQueue = new();

            projectileQueue.Add(p);
        }
    }
    public interface IQuakeHitable
    {
        public GameObject hitGameObject { get; }
        public bool IsPlayer => hitGameObject.TryGetComponent(out IFumoUnit u) && u.IsPlayer;
        public struct HitPacket
        {
            public Vector3 HitPoint;
            public float Damage;
            public ITargetting Sender;
        }
        public void Hit(HitPacket packet);
    }
    public class QuakeProjectileRenderer : MonoBehaviour
    {
        List<QuakeProjectile> activeProjectiles = new();
        [SerializeField] List<ParticleSystem> particleIndex = new();
        [SerializeField] ParticleSystem CollisionParticleCopy;
        [SerializeField] LayerMask EnemyHitMask, PlayerHitMask;
        public static Transform Observer;

        static ParticleSystem cachedCollisionParticle;
        private void OnEnable()
        {
            cachedCollisionParticle = CollisionParticleCopy;
        }
        private void OnDisable()
        {
            if (cachedCollisionParticle = CollisionParticleCopy)
            {
                cachedCollisionParticle = null;
            }
        }
        public static void ProjHitEffect(RaycastHit hit)
        {
            if (cachedCollisionParticle is ParticleSystem ps)
            {
                ParticleSystem terrainHit = Instantiate(ps, hit.point, Quaternion.identity);
                terrainHit.transform.LookAt(hit.point + hit.normal);
                terrainHit.Play();
            }
        }
        public bool ProjectileStepAndRaycast(QuakeProjectile proj, out IQuakeHitable hitable)
        {
            hitable = null;
            LayerMask m = EnemyHitMask;
            switch (proj.Faction)
            {
                case QuakeFaction.Default:
                    return false;
                case QuakeFaction.Player:
                    break;
                case QuakeFaction.Enemy:
                    m = PlayerHitMask;
                    break;
                default:
                    break;
            }
            Vector3 movement = proj.EffectiveVelocity * Time.deltaTime;
            Vector3 start = proj.FinalizedPosition;

            if (Physics.Raycast(start, movement.normalized, out RaycastHit hit, movement.magnitude, m, QueryTriggerInteraction.Ignore))
            {
                ProjHitEffect(hit);
                proj._currentPosition = hit.point;
                if (hit.transform.TryGetComponent(out IQuakeHitable target) || hit.transform.root.TryGetComponent(out target))
                {
                    hitable = target;
                    proj.HitAction?.Invoke(QuakeProjectile.HitActionResult.IHit);
                    return true;
                }
                proj.HitAction?.Invoke(QuakeProjectile.HitActionResult.Terrain);
                return true;
            }
            else
            {
                proj._currentPosition += proj.EffectiveVelocity * Time.deltaTime;
            }
            return false;
        }
        Vector3 observerPosition;
        List<Vector3> particlePositions = new();
        private void Update()
        {
            if (Observer != null) observerPosition = Observer.transform.position;
            activeProjectiles.RemoveAll(x => x == null || x.QueueRemoval || x.FinalizedPosition.SquareDistanceToGreaterThan(observerPosition, 150f));
            if (QuakeProjectile.projectileQueue == null)
                QuakeProjectile.projectileQueue = new();

            foreach (var projectile in QuakeProjectile.projectileQueue)
            {
                activeProjectiles.Add(projectile);
            }
            QuakeProjectile.projectileQueue.Clear();
            for (int i = 0; i < particleIndex.Count; i++)
            {
                particlePositions.Clear();
                IEnumerable<QuakeProjectile> iteration = activeProjectiles.Where(x => x.Channel == i);
                foreach (var proj in iteration)
                {
                    if (proj.QueueRemoval)
                        continue;
                    if (ProjectileStepAndRaycast(proj, out IQuakeHitable hit))
                    {
                        bool HitComponent = hit != null;
                        QuakeProjectile.ScheduleClear(proj);
                        if (HitComponent)
                        {
                            hit.Hit(proj.HitPacket);
                        }
                    }
                    particlePositions.Add(proj.FinalizedPosition);
                }
                ParticleSystem ps = particleIndex[i];
                ps.RenderAnimatedPoints_3D(particlePositions, 2f, true);

            }
        }
    }
}
