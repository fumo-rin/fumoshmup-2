using UnityEngine;
using rinCore;
using System.Linq;

namespace FumoQuake
{
    public class Coward : QuakeEnemy, IQuakeHitable
    {
        float fleeDistance = 20f;
        
        public GameObject hitGameObject => gameObject;
        public void Hit(IQuakeHitable.HitPacket packet)
        {
            float damageTaken = packet.Damage.Clamp(0f, CurrentHealth);
            CurrentHealth -= damageTaken;
            if (CurrentHealth < 0f + Mathf.Epsilon)
            {
                Destroy(gameObject);
            }
        }
        void Targetting(ITargetting target)
        {
            if (target != null && target.TargetActive && Time.time > RandomAttackTime)
            {
                Transform t = target.RandomOrderedTargets.First();

                if (gun != null)
                {
                    Vector3 origin = transform.position + new Vector3(0f, 0.75f, 0f);
                    Ray targetRay = new()
                    {
                        direction = t.position - origin,
                        origin = origin
                    };
                    gun.TryShootWith(targetRay);
                    RandomAttackTime = gun.RandomAggressionTimeAfterLock(0.6f, 1.25f);
                }
            }
        }
        protected override void WhenThink(ITargetting target, IFumoUnit targetUnit, float dt)
        {
            bool targetTooFar = targetUnit != null && targetUnit.CurrentPosition.SquareDistanceToGreaterThan(transform.position, 35f);
            if (targetTooFar)
                return;

            if (targetUnit != null)
            {
                if (targetUnit.CurrentPosition.SquareDistanceToGreaterThan(transform.position, fleeDistance))
                {
                    Pathing(ref nextPathTick, Path_DirectlyTowards, targetUnit);
                }
                else
                {
                    Pathing(ref nextPathTick, Path_AwayFrom, targetUnit);
                }
            }
            Targetting(target);
            if (navigation.Nav.HasDestination && navigation.rb.linearVelocity.Y(0f).magnitude.Absolute() < 2f && grounded.IsGrounded)
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