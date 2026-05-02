using rinCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FumoShmup2
{
    #region Overrides
    public class RevengeAttackOverride
    {
        RevengeAttack revenge;
        public RevengeAttackOverride(RevengeAttack r)
        {
            this.revenge = r;
        }
        public void TriggerRevenge(ShmupUnit Owner)
        {
            if (Owner == null)
            {
                return;
            }
            Vector2 target = Owner.CurrentPosition + Vector2.down.ScaleToMagnitude(10f);
            Vector2 rbVelocity = Owner.RB.linearVelocity;
            if (ShmupPlayer.PlayerAs(out ShmupPlayer p))
            {
                target = p.CurrentPosition;
            }
            revenge.TriggerAttack(Owner.CurrentPosition, target, rbVelocity);
        }
    }
    #endregion
    #region Shortcuts
    public partial class RevengeAttack
    {
        public static Projectile.ArcSettings Arc(float centerAimAngle, float arcSize, int shotCount, float projectileSpeed) => ProjectileFactory.Arc(centerAimAngle, arcSize, shotCount, projectileSpeed);
        public static Projectile.SingleSettings Single(float addedAngle, float projectileSpeed)
            => ProjectileFactory.Single(addedAngle, projectileSpeed);

        public static Projectile.CircleSettings Circle(float addedAngle, int segments, float projectileSpeed)
            => ProjectileFactory.Circle(addedAngle, segments, projectileSpeed);

        public class WaitForSweepOrTime : CustomYieldInstruction
        {
            private readonly float endTime;
            private readonly WaitForSeconds pooledWait;
            public WaitForSweepOrTime(float seconds)
            {
                endTime = Time.time + seconds;
                pooledWait = seconds.WaitForSeconds();
            }
            public override bool keepWaiting
            {
                get
                {
                    if (ProjectileRunner.IsSweeping)
                        return false;

                    return Time.time < endTime;
                }
            }
        }
    }
    #endregion
    #region Bitmask Difficulty
    public partial class RevengeAttack
    {
        public bool IsActiveForCurrentDifficulty => true;
    }
    #endregion
    public abstract partial class RevengeAttack : ScriptableObject
    {
        protected static List<Projectile> iterationList;
        public void TriggerAttack(Vector2 origin, Vector2 target, Vector2 rbVelocity)
        {
            if (!IsActiveForCurrentDifficulty)
                return;
            Projectile.InputSettings input = new(origin, null, target - origin, new(null, 1f, 1f), ProjectileFaction.Enemy);
            Projectile.InputSettings rbInput = new(origin, null, rbVelocity, new(null, 1f, 1f), ProjectileFaction.Enemy);
            AttackPayload(input, rbInput.Direction == Vector2.zero ? input : rbInput);
        }
        protected void StartRevengeAttackRoutine(IEnumerator co)
        {
            GlobalCoroutineRunner.StartRoutine("Revenge Attack", co);
        }
        public static void StopAll()
        {
            GlobalCoroutineRunner.StopAllOfKey("Revenge Attack");
        }
        protected abstract void AttackPayload(Projectile.InputSettings input, Projectile.InputSettings rbInput);
    }
}
