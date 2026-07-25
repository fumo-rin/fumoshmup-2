using rinCore;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace FumoQuake
{
    public class Tewi : QuakeEnemy, IQuakeHitable, ITargetting
    {
        float nextJumpTime;
        float jumpDisableTimer;
        [SerializeField] ACWrapper boing;
        public GameObject hitGameObject => gameObject != null ? gameObject : null;
        public new bool TargetActive => hitGameObject.activeInHierarchy;
        public new IEnumerable<Transform> RandomOrderedTargets => new List<Transform>() { transform };
        Coroutine chargeJump;
        void JumpPathing(IFumoUnit other)
        {
            if (!grounded.IsGrounded || jumpDisableTimer > 0f) return;

            if (other == null || !other.IsAlive)
            {
                if (navigation.Nav.HasDestination)
                    return;
                if (navigation.Nav.TryProjectToNavmesh(lastKnownTarget + RNG.SeededRandomInsideUnitSphere * 3f, out Vector3 celebration, 5f))
                {
                    navigation.SetNewTarget(celebration);
                    navigation.rb.linearVelocity = new Vector3(UnitRB.linearVelocity.x, 4f, UnitRB.linearVelocity.z) + RNG.SeededRandomInsideUnitSphere;
                }
                return;
            }

            bool canSee = CanSee(target.FirstTargetPosition, 12f, out ITargetting visibleTarget);

            bool canReach = navigation.Nav.CanReach(CurrentPosition, other.CurrentPosition);
            float sqrDistance = (Center - other.CurrentPosition).sqrMagnitude;
            bool withinLeapRange = sqrDistance > 9f;
            bool shouldWalkChase = canSee && canReach && withinLeapRange;
            if (!shouldWalkChase && Time.time >= nextJumpTime)
            {
                nextJumpTime = Time.time + RNG.FloatRange(1.25f, 2.75f);

                Vector3 targetImpactPoint = (visibleTarget != null) ? visibleTarget.FirstTargetPosition : other.CurrentPosition;

                if (UnitRB != null)
                {
                    Vector3 flatDir = (targetImpactPoint - CurrentPosition).Y(0f).ScaleToMagnitude(RNG.FloatRange(10f, 13f));
                    Vector3 jumpVelocity = flatDir + new Vector3(0f, RNG.FloatRange(5.5f, 8f), 0f);

                    navigation.Nav.StopPath();
                    navigation.Nav.enabled = false;

                    jumpDisableTimer = 0.15f;

                    boing.Play(Center);
                    UnitRB.linearVelocity = jumpVelocity;
                }
                return;
            }

            if (!navigation.Nav.TryProjectToNavmesh(other.CurrentPosition, out Vector3 navPos, 5f))
            {
                if (NavMesh.FindClosestEdge(other.CurrentPosition, out NavMeshHit hit, NavMesh.AllAreas))
                    navPos = hit.position;
                else
                {
                    navigation.Nav.StopPath();
                    return;
                }
            }

            navigation.SetNewTarget(navPos);
            lastKnownTarget = navPos;
        }

        bool CollideWith(IFumoUnit targetUnit)
        {
            if (target != null && target.TargetActive && targetUnit != null && Center.SquareDistanceToLessThan(targetUnit.Center, 2f))
            {
                GeneralManager.FunnyExplosion(new()
                {
                    is3d = true,
                    playSound = true,
                    position = Center,
                    scale = 2.5f
                });

                foreach (var item in Physics.OverlapSphere(Center, 7f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    Vector3 rawDirection = item.bounds.center - Center;
                    if (rawDirection.sqrMagnitude < 0.001f) continue;

                    Vector3 direction = rawDirection.normalized;
                    Ray explosiveRay = new Ray(Center, direction);

                    if (Physics.Raycast(explosiveRay, out RaycastHit damageHit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (damageHit.transform.TryGetComponent(out IQuakeHitable hitable) && hitable.IsPlayer)
                        {
                            float distanceToHit = (damageHit.point - Center).magnitude;
                            float baseDamage = 40f + (20f - distanceToHit.Clamp(0f, 4f));
                            hitable.Hit(new()
                            {
                                Damage = baseDamage.Multiply(RNG.FloatRange(0.5f, 1f)),
                                HitPoint = damageHit.point,
                                Sender = this,
                            });
                        }
                    }

                    if (Physics.Raycast(explosiveRay, out RaycastHit launchHit, 7f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        float launchDistance = (launchHit.point - Center).magnitude;
                        float lerp01 = launchDistance.MapTo01(4f, 12f).Clamp(0f, 1f);
                        float calculatedForce = lerp01.MapFrom01(45f, 70f);

                        launchHit.collider.AddImpactVelocity(new Impact(launchHit, explosiveRay, calculatedForce));
                    }
                }

                IsAlive = false;
                Destroy(gameObject);
                return true;
            }
            return false;
        }

        protected override void WhenThink(ITargetting target, IFumoUnit targetUnit, float dt)
        {
            if (jumpDisableTimer > 0f)
            {
                jumpDisableTimer -= dt;
            }

            if (CollideWith(targetUnit))
            {
                return;
            }

            if (!grounded.IsGrounded || jumpDisableTimer > 0f)
            {
                if (UnitRB.linearVelocity.y > -30f)
                {
                    UnitRB.linearVelocity += Vector3.down * dt * 4f;
                }
                return;
            }
            else
            {
                if (!navigation.Nav.enabled)
                {
                    navigation.Nav.enabled = true;
                }
            }

            bool hasChaseTarget = targetUnit != null && target != null;
            if (hasChaseTarget)
                Pathing(ref nextPathTick, JumpPathing, targetUnit);
        }

        public void Hit(IQuakeHitable.HitPacket packet)
        {
            HitProcessing.ProcessHit(this, packet, HitProcessing.KillExplosion);
        }

        protected override void WhenAwake() { }
        protected override void WhenDisable() { }
        protected override void WhenEnable() { }
        protected override void WhenStart() { }
    }
}