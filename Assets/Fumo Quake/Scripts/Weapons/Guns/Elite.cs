using rinCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoQuake
{
    public class Elite
    {
        [System.Serializable]
        public class EliteRingFairy : BaseGun, IQuakeShooter
        {
            [System.Serializable]
            public struct Data
            {
                public int projectileIndex;
                public int pellets;
                [SerializeField] List<float> pelletRandomDamage;
                public float projectileSpeed;
                public float RandomDamage => pelletRandomDamage.RandomResult() is float f && f > 0f ? f : 10f;
            }
            public Data ring;
            public override bool IsProjectileWeapon => true;
            public void Shoot(WeaponsController runner, ITargetting sender, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                float totalSequenceDuration = 4f * 0.5f;
                weaponLock.Stall(totalSequenceDuration + WeaponShootLockTime);

                IEnumerator CO_Run(Vector3 startPos)
                {
                    Ray r = RinHelper.RayDot(ray, 0.001f);
                    for (int i = 0; i < 5; i++)
                    {
                        if (runner == null) yield break;

                        Vector3 unitOffset = runner.transform.position - startPos;
                        GunSound.Play(ray.origin);

                        float stepSpacing = 360f / ring.pellets.AsFloat().Max(8f);
                        float dynamicRotationOffset = i.AsFloat(5f).SpreadWithNegative(100f);
                        foreach (var baseAngle in stepSpacing.StepFromTo(0f, 360f).Take(100))
                        {
                            float finalAngle = baseAngle + dynamicRotationOffset;

                            Ray rClone = r.RotateRelative(0f, finalAngle);
                            QuakeProjectile.CreateProjectile(new()
                            {
                                Direction = rClone.direction,
                                Faction = OwnerFaction,
                                Origin = rClone.origin + unitOffset,
                                Speed = ring.projectileSpeed
                            }, sender, out QuakeProjectile p);

                            if (p != null)
                            {
                                p.Channel = ring.projectileIndex;
                                p.Damage = ring.RandomDamage;
                            }
                        }

                        if (i < 4)
                        {
                            yield return 0.6f.WaitForSeconds();
                        }
                    }
                }

                runner.StartCoroutine(CO_Run(runner.transform.position));
                SetNewLockTimes(ref weaponLock);
            }

            public override BaseGun Clone()
            {
                return new EliteRingFairy()
                {
                    GunSound = GunSound,
                    WeaponShootLockTime = WeaponShootLockTime,
                    IsLocked = IsLocked,
                    optionalIconUI = optionalIconUI,
                    OwnerFaction = OwnerFaction,
                    ring = ring,
                    WeaponShootSwapLockDuration = WeaponShootSwapLockDuration,
                };
            }
        }
    }
}