using System.Collections;
using UnityEngine;

namespace FumoShmup2
{
    public abstract class DamageTakenAttack : MonoBehaviour
    {
        [SerializeField] protected EnemyUnit Owner;
        protected readonly float TICK = 0.015f;
        protected AttackBuilder a = new();
        protected abstract void WhenDamaged(float actualDamage, IHit.HitPacket packet);
        protected void BuildInput(out Projectile.InputSettings result)
        {
            a.BuildInput(Owner, out result);
        }
        protected virtual void WhenEnable()
        {

        }
        protected virtual void WhenDisable()
        {

        }
        private void OnEnable()
        {
            Owner.WhenDamaged += WhenDamaged;
            WhenEnable();
        }
        private void OnDisable()
        {
            Owner.WhenDamaged -= WhenDamaged;
            WhenDisable();
        }
    }
}
