using FumoShmup2;
using rinCore;
using System.Collections;
using UnityEngine;

namespace pride
{
    public class PrideShotgun : ShmupPlayerShooter
    {
        [SerializeField] ProjectileDefineSO shotgunProj;
        [SerializeField] float shotgunDamage = 200f;
        protected override void WhenEnable()
        {

        }
        protected override void WhenUpdate()
        {
            IEnumerator CO_Shotgun(Projectile.InputSettings input)
            {
                void Shoot(Projectile.InputSettings input)
                {
                    for (int i = 0; i < 40; i++)
                    {
                        if (a.Single(RNG.FloatRange(-14f, 14f), RNG.FloatRange(22f, 34f)).Spawn(input, shotgunProj, out Projectile p))
                        {
                            p.SetDamage(new(Owner, (shotgunDamage.Add(1f) / 40f).Floor(), 1f));
                        }
                    }
                }
                Shoot(input);
                LockedAttackTime = Time.time + 0.4f;
                float attackLock = 0.05f + Time.time;
                yield return TICK.WaitForSeconds(3);
                while (Time.time < LockedAttackTime)
                {
                    if (this.ShootState.PowerFireTap && Time.time > attackLock)
                    {
                        input.Reposition();
                        Shoot(input);
                        LockedAttackTime = Time.time + 0.4f;
                        attackLock = 0.05f + Time.time;
                    }
                    yield return TICK.WaitForSeconds();
                }
                CurrentShotAction = null;
            }
            if (ShootState.PowerFire && !LockedAttack && a.BuildInput(Owner, out Projectile.InputSettings input))
            {
                TryStartShot(CO_Shotgun(input), ref CurrentShotAction, true);
            }
        }
    }
}
