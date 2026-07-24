using rinCore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoQuake
{
    public class Fairy : QuakeEnemy, IQuakeHitable, IStrafe, ITargetting
    {
        [SerializeField] private StrafeController strafeSystem = new();
        public bool TryStrafe(ref Vector3 velocity, ITargetting target)
        {
            if (!CanSee(target))
                return false;
            return strafeSystem.TryRunStrafe(transform, ref velocity, target);
        }
        public WeaponsController gun2;
        public GameObject hitGameObject => gameObject;

        public new bool TargetActive => IsAlive;

        public new IEnumerable<Transform> RandomOrderedTargets
        {
            get
            {
                yield return transform;
            }
        }

        public void Hit(IQuakeHitable.HitPacket packet)
        {
            HitProcessing.ProcessHit(this, packet);
        }
        void Targetting(ITargetting target)
        {
            if (target == null || !target.TargetActive || Stalled) return;
            if (Time.time < RandomAttackTime) return;
            Transform t = target.RandomOrderedTargets.FirstOrDefault();
            if (t == null) return;

            float targetDistance = t.transform.position.DistanceTo(CurrentPosition);
            bool canSee = CanSee(target);

            if (!canSee)
            {
                float stallDuration = RNG.FloatRange(0.15f, 0.35f);
                gun.weaponLockTiming.Stall(stallDuration);
                gun2.weaponLockTiming.Stall(stallDuration);

                RandomAttackTime = Time.time + stallDuration;
                return;
            }

            Vector3 origin = transform.position + new Vector3(0f, 0.75f, 0f);
            Ray targetRay = new()
            {
                direction = (t.position - origin).normalized,
                origin = origin
            };

            if (targetDistance < 10f)
            {
                if (gun != null && gun.TryShootWith(this, targetRay))
                {
                    float l = gun != null && gun.CurrentWeapon != null && gun.CurrentWeapon.WeaponShootLockTime is float time ? time : 1f;
                    RandomAttackTime = Time.time + l + RNG.FloatRange(1f, 1.7f);
                }
            }
            else if (targetDistance > 16f)
            {
                if (gun2 != null && gun2.TryShootWith(gun2.CurrentWeapon, this, targetRay))
                    RandomAttackTime = Time.time + RNG.FloatRange(0.4f, 0.7f);
            }
            else
            {
                if (gun2 != null && gun2.TryShootWith(this, targetRay))
                {
                    RandomAttackTime = Time.time + 0.2f;
                }
            }
        }
        protected override void WhenThink(ITargetting target, IFumoUnit targetUnit, float dt)
        {
            bool hasChaseTarget = targetUnit != null && target != null;

            bool gunsReady = gun.weaponLockTiming.CanShoot;
            if (hasChaseTarget)
            {
                if (gunsReady)
                    Pathing(ref nextPathTick, Path_DirectlyTowards, targetUnit);
                else
                {
                    Pathing(ref nextPathTick, strafeSystem.Path_TryStrafeThenPathTowards, targetUnit);
                }
            }

            Targetting(target);
            //if (hasChaseTarget && navigation.Nav.HasDestination && navigation.rb.linearVelocity.Y(0f).magnitude.Absolute() < 2f && grounded.IsGrounded)
            //{
            //    navigation.rb.linearVelocity = new Vector3(0f, 4f, 0f) + RNG.SeededRandomInsideUnitSphere;
            //}
        }

        protected override void WhenAwake()
        {

        }

        protected override void WhenStart()
        {

        }

        protected override void WhenEnable()
        {

        }

        protected override void WhenDisable()
        {

        }
    }
}
