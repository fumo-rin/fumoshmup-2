using rinCore;
using UnityEngine;

namespace FumoQuake
{
    public class PipebombDemon : QuakeEnemy, IQuakeHitable, ITargetting
    {
        public GameObject hitGameObject => gameObject;
        public void Hit(IQuakeHitable.HitPacket packet)
        {
            if (packet.Sender is IFumoUnit unit && unit.IsAlive && Tewi_TryCollideWith(unit, true))
            {
                unit.IsAlive = false;
            }
            HitProcessing.ProcessHit(this, new()
            {
                Damage = CurrentHealth,
                HitPoint = packet.HitPoint,
                Sender = packet.Sender,
            });
        }
        protected override void WhenAwake()
        {

        }
        protected override void WhenDisable()
        {

        }
        protected override void WhenEnable()
        {

        }

        protected override void WhenStart()
        {

        }
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.transform.TryGetComponent(out PipebombDemon _) && collision.transform.TryGetComponent(out IFumoUnit unit) && Tewi_TryCollideWith(unit))
            {

            }
        }
        float extraTick;
        protected override void WhenThink(ITargetting target, IFumoUnit targetUnit, float dt)
        {
            if (Time.time > extraTick)
            {
                if (targetUnit.IsPlayer && targetUnit is ITargetting t)
                {
                    Action_LockTarget(t, 5f);
                }
                extraTick = Time.time + 0.5f;
            }
            if (Tewi_TryCollideWith(targetUnit, target != null && target.TargetActive
                && Center.SquareDistanceToLessThan(targetUnit.Center.Z(target.Center.z), 0.5f)
                && Center.SquareDistanceToLessThan(target.Center, 3f)))
            {
                return;
            }
            if (!grounded.IsGrounded)
            {
                if (UnitRB.linearVelocity.y > -30f)
                {
                    UnitRB.linearVelocity += Vector3.down * dt * 4f;
                }
                return;
            }
        }
    }
}
