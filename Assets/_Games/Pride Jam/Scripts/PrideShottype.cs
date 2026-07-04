using FumoShmup2;
using rinCore;
using System.Collections;
using UnityEngine;

namespace pride
{
    public class PrideShottype : ShmupPlayerShooter
    {
        [SerializeField] ProjectileDefineSO unfocusProjectile;
        protected override void WhenEnable()
        {

        }
        protected override void WhenUpdate()
        {
            IEnumerator CO_Unfocus(Projectile.InputSettings input)
            {
                bool firstShot = true;
                while (ShootState.Shooting)
                {
                    if (shotcap.Count > 7)
                    {
                        yield return TICK.WaitForSeconds(1);
                        continue;
                    }
                    const float shotDamageMod = 1 / 63f;
                    if (firstShot)
                    {
                        if (a.Single(0f, 44f).Spawn(input, unfocusProjectile, out Projectile p))
                        {
                            shotcap.Add(p);
                            p.SetDamage(new(Owner, shotDamageMod * 6 * Dps_Unfocus, 1f));
                        }
                        yield return TICK.WaitForSeconds(6);
                    }
                    firstShot = false;

                    input.SetOrigin(Owner.CurrentPosition + new Vector2(-0.2f, 0.25f));
                    if (a.Single(0f, 44f).Spawn(input, unfocusProjectile, out Projectile p1))
                    {
                        shotcap.Add(p1);
                        p1.SetDamage(new(Owner, shotDamageMod * Dps_Unfocus, 1f));
                    }
                    input.SetOrigin(Owner.CurrentPosition + new Vector2(0.2f, 0.25f));
                    if (a.Single(0f, 44f).Spawn(input, unfocusProjectile, out Projectile p2))
                    {
                        shotcap.Add(p2);
                        p2.SetDamage(new(Owner, shotDamageMod * Dps_Unfocus, 1f));
                    }
                    yield return TICK.WaitForSeconds(2);
                }
                CurrentShotAction = null;
            }
            if (ShootState.Shooting && CurrentShotAction == null)
            {
                a.BuildInput(Owner, out Projectile.InputSettings input);
                TryStartShot(CO_Unfocus(input), ref CurrentShotAction, false);
            }
        }
    }
}
