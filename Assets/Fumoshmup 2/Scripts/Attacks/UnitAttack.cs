using UnityEngine;
using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace FumoShmup2
{
    #region Targetting Helper
    public class AttackBuilder
    {
        public Projectile.ArcSettings Arc(float centerAimAngle, float arcSize, int shotCount, float projectileSpeed)
            => ProjectileFactory.Arc(centerAimAngle, arcSize, shotCount, projectileSpeed);

        public Projectile.SingleSettings Single(float addedAngle, float projectileSpeed)
            => ProjectileFactory.Single(addedAngle, projectileSpeed);

        public Projectile.CircleSettings Circle(float addedAngle, int segments, float projectileSpeed)
            => ProjectileFactory.Circle(addedAngle, segments, projectileSpeed);

        public bool BuildInput(ShmupUnit Sender, out Projectile.InputSettings result)
        {
            result = default;
            if (Sender == null)
                return false;

            Vector2 origin = Sender.CurrentPosition;
            if (Sender is EnemyUnit e)
            {
                bool foundPlayer = ShmupPlayer.PlayerAs(out ShmupPlayer p);
                result = new Projectile.InputSettings(origin, Sender, Vector2.down, new(Sender, 1f, 1f), ProjectileFaction.Enemy);
                if (foundPlayer)
                {
                    if (p.IsAlive)
                    {
                        result.AimTo(p);
                    }
                    result.AssignTarget(p);
                }
            }
            else if (Sender is ShmupPlayer player)
            {
                result = new Projectile.InputSettings(origin, Sender, Vector2.up, new(Sender, 1f, 1f), ProjectileFaction.Player);
                result.Flare = false;
                if (EnemyUnit.FindEnemyFromDotProduct(player.CurrentPosition, Vector2.up, out EnemyUnit autoTarget, 0.25f))
                    result.AssignTarget(autoTarget);
            }
            return true;
        }
    }
    #endregion
    #region Shortcuts
    public partial class UnitAttack
    {
        public Projectile.ArcSettings Arc(float centerAimAngle, float arcSize, int shotCount, float projectileSpeed)
            => ProjectileFactory.Arc(centerAimAngle, arcSize, shotCount, projectileSpeed);

        public Projectile.SingleSettings Single(float addedAngle, float projectileSpeed)
            => ProjectileFactory.Single(addedAngle, projectileSpeed);

        public Projectile.CircleSettings Circle(float addedAngle, int segments, float projectileSpeed)
            => ProjectileFactory.Circle(addedAngle, segments, projectileSpeed);
    }
    #endregion
    #region Extra Render
    public partial class UnitAttack
    {
        protected virtual void RunExtrasUpdate(ShmupUnit Sender) { }
    }
    #endregion

    [System.Serializable]
    public abstract partial class UnitAttack
    {
        protected static List<Projectile> iterationList;
        protected bool PlayerAutoAim(ShmupPlayer player, out EnemyUnit autoTarget)
        {
            autoTarget = null;
            if (EnemyUnit.FindEnemyFromDotProduct(player.CurrentPosition, Vector2.up, out autoTarget, 0.25f))
            {
                return true;
            }
            return false;
        }
        private IEnumerator DEPRECATED_TryAttack(ShmupUnit Sender)
        {
            Vector2 origin = Sender.CurrentPosition;
            if (Sender is EnemyUnit e)
            {
                bool foundPlayer = ShmupPlayer.PlayerAs(out ShmupPlayer p);
                var input = new Projectile.InputSettings(origin, Sender, Vector2.down, new(Sender, 1f, 1f), ProjectileFaction.Enemy);

                if (foundPlayer)
                {
                    if (p.IsAlive)
                    {
                        input.AimTo(p);
                    }
                    input.AssignTarget(p);
                }
                //yield return e.AddQueuedAttack(TEMPORARY_Attackpayload(e, input), false);
            }
            else if (Sender is ShmupPlayer player)
            {
                var input = new Projectile.InputSettings(origin, Sender, Vector2.up, new(Sender, 1f, 1f), ProjectileFaction.Player);
                //input.Flare = false;
                if (EnemyUnit.FindEnemyFromDotProduct(player.CurrentPosition, Vector2.up, out EnemyUnit autoTarget, 0.25f))
                    input.AssignTarget(autoTarget);

                //yield return player.AddQueuedAttack(TEMPORARY_Attackpayload(player, input), false);
            }
            yield break;
        }
        public Coroutine StartWithSender(ShmupUnit Sender, Action callback = null)
        {
            if (Sender == null)
                return null;

            Vector2 origin = Sender.CurrentPosition;
            if (Sender is EnemyUnit e)
            {
                bool foundPlayer = ShmupPlayer.PlayerAs(out ShmupPlayer p);
                var input = new Projectile.InputSettings(origin, Sender, Vector2.down, new(Sender, 1f, 1f), ProjectileFaction.Enemy);

                if (foundPlayer)
                {
                    if (p.IsAlive)
                    {
                        input.AimTo(p);
                    }
                    input.AssignTarget(p);
                }
                return Sender.StartCoroutineWithCallback(CO_Attackpayload(e, input), callback);
            }
            else if (Sender is ShmupPlayer player)
            {
                var input = new Projectile.InputSettings(origin, Sender, Vector2.up, new(Sender, 1f, 1f), ProjectileFaction.Player);
                input.Flare = false;
                if (EnemyUnit.FindEnemyFromDotProduct(player.CurrentPosition, Vector2.up, out EnemyUnit autoTarget, 0.25f))
                    input.AssignTarget(autoTarget);

                return Sender.StartCoroutineWithCallback(CO_Attackpayload(player, input), callback);
            }
            return null;
        }
        protected abstract IEnumerator CO_Attackpayload(ShmupUnit sender, Projectile.InputSettings input);
    }
}
