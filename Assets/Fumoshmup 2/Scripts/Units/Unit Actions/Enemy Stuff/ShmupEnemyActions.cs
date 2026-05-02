using UnityEngine;
using UnityEngine.Events;
using rinCore;

namespace FumoShmup2
{
    [System.Serializable]
    public class ShmupEnemyActions
    {

        [System.Serializable]
        public class ChasePlayerDownScreen : UnitAction
        {
            float startTime;
            public FumoUnit chaseTarget;
            float remainingDuration;
            float speed;
            public ChasePlayerDownScreen(FumoUnit owner, FumoUnit chaseTarget, float maxDuration, float expiringDuration, float speed) : base(owner, expiringDuration)
            {
                this.speed = speed;
                this.remainingDuration = maxDuration;
                this.startTime = Time.time;
                this.chaseTarget = chaseTarget;
            }
            protected override ActionResult RunAction(FumoUnit Owner)
            {
                ActionResult result = ActionResult.Performed;
                remainingDuration -= Time.deltaTime;
                Vector2 chaseDirection = Vector2.down * speed;
                if (remainingDuration > 0f)
                {
                    if (chaseTarget != null && chaseTarget is FumoUnit c && c.IsAlive)
                    {
                        if (c.CurrentPosition.SquareDistanceToGreaterThan(Owner.CurrentPosition, 2f) && (Owner.CurrentPosition.y - c.CurrentPosition.y) > 1.25f)
                        {
                            chaseDirection = (c.CurrentPosition - Owner.CurrentPosition).normalized;
                        }
                        else
                        {
                            chaseTarget = null;
                        }
                    }
                }
                else if (Owner.RB.linearVelocity.y < 0f)
                {
                    chaseDirection = Owner.RB.linearVelocity;
                }
                Owner.RB.VelocityTowards(chaseDirection * speed, 12f);

                ShmupWorldspace.MapToWorldspaceUnclamped(0.5f, -0.2f, out Vector2 belowScreen);
                if (Owner.CurrentPosition.y < belowScreen.y && Owner is EnemyUnit enemy)
                {
                    enemy.ForceKill();
                }
                if (Owner is EnemyUnit e)
                {
                    e.SetLeashPosition(e.CurrentPosition);
                }
                return result;
            }
            public override bool IsRunning()
            {
                return Time.time > startTime && duration > 0;
            }
        }
    }
}
