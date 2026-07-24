using UnityEngine;
using rinCore;
using System.Linq;
using System;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using NUnit.Framework;
namespace FumoQuake
{
    public partial class Imp : QuakeEnemy, IQuakeHitable, IStrafe
    {
        [SerializeField] private StrafeController strafeSystem = new();
        public bool TryStrafe(ref Vector3 velocity, ITargetting target)
        {
            if (!CanSee(target))
                return false;
            return strafeSystem.TryRunStrafe(transform, ref velocity, target);
        }
        public GameObject hitGameObject => gameObject;
        public void Hit(IQuakeHitable.HitPacket packet)
        {
            HitProcessing.ProcessHit(this, packet);
        }
        void Targetting(ITargetting target)
        {
            if (target != null && target.TargetActive && Time.time > RandomAttackTime)
            {
                Transform t = target.RandomOrderedTargets.First();
                float targetDistance = t.transform.position.DistanceTo(CurrentPosition);
                if (gun != null && !Stalled)
                {
                    Vector3 origin = transform.position + new Vector3(0f, 0.75f, 0f);
                    Ray targetRay = new()
                    {
                        direction = t.position - origin,
                        origin = origin
                    };
                    void ShootLoop(bool canSee)
                    {
                        float stallduration = 0.5f;
                        StallCalculation(ref stallduration);
                        void StallCalculation(ref float stallTime)
                        {
                            if (targetDistance < 5f)
                            {
                                stallTime = 0.15f;
                                RandomAttackTime = gun.RandomAggressionTimeAfterLock(0.4f, 0.8f);
                            }
                            else if (targetDistance < 12f)
                            {
                                RandomAttackTime = gun.RandomAggressionTimeAfterLock(0.8f, 1.35f);
                            }
                            else if (targetDistance < 22f)
                            {
                                stallTime = 0f;
                                RandomAttackTime = gun.RandomAggressionTimeAfterLock(1.35f, 1.85f);
                            }
                            else
                            {
                                stallTime = 0f;
                                RandomAttackTime = gun.RandomAggressionTimeAfterLock(0.3f, 0.6f);
                            }
                        }
                        if (!canSee)
                        {
                            gun.weaponLockTiming.Stall(RNG.FloatRange(0.15f, 0.35f));
                        }
                        if (gun.TryShootWith(this, targetRay) && stallduration > 0f)
                        {
                            StallTimeEnd = stallduration + Time.time;
                        }
                    }
                    ShootLoop(CanSee(target));
                }
            }
        }
        protected override void WhenThink(ITargetting target, IFumoUnit targetUnit, float dt)
        {
            bool hasChaseTarget = targetUnit != null && target != null;

            if (hasChaseTarget)
                Pathing(ref nextPathTick, strafeSystem.Path_TryStrafeThenPathTowards, targetUnit);

            Targetting(target);
            if (hasChaseTarget && navigation.Nav.HasDestination && navigation.rb.linearVelocity.Y(0f).magnitude.Absolute() < 2f && grounded.IsGrounded)
            {
                navigation.rb.linearVelocity = new Vector3(0f, 4f, 0f) + RNG.SeededRandomInsideUnitSphere;
            }
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
