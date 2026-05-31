using rinCore;
using System.Collections;
using UnityEngine;

namespace FumoShmup2
{
    public class FutariTowerRevenge : DamageTakenAttack
    {
        float cumulativeDamage = 0f;
        [SerializeField] float shootEveryDamage = 5f;
        [SerializeField] ProjectileDefineSO towerProjectile;
        [SerializeField] Transform towerOrigin;
        protected override void WhenDamaged(float actualDamage, IHit.HitPacket packet)
        {
            cumulativeDamage += actualDamage;
        }
        IEnumerator CO_Run()
        {
            BuildInput(out Projectile.InputSettings input);
            while (Owner != null && Owner.IsAlive)
            {
                if (!Owner.IsOnScreenAndAlive)
                {
                    yield return null;
                    continue;
                }
                while (cumulativeDamage > 25f)
                {
                    BuildInput(out input);
                    input.addedForward = 0.35f;
                    for (int i = 0; i < 8; i++)
                    {
                        input.ReAimWithOptionalTarget(towerOrigin.position);
                        float randomAngle = cumulativeDamage.MapTo01(20f, 150f, true).MapFrom01(3f, 35f);
                        if (a.Circle(randomAngle.SpreadWithNegative(100f), 6, 2f + i.AsFloat(1.15f)).Spawn(input, towerProjectile, out _))
                        {
                            cumulativeDamage -= 25f / 8f;
                        }
                    }
                    yield return TICK.WaitForSeconds(1);
                }
                yield return null;
            }
        }
        protected override void WhenEnable()
        {
            Owner.StartCoroutine(CO_Run());
        }
    }
}