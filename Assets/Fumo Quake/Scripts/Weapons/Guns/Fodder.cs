using UnityEngine;
using rinCore;
using System.Collections.Generic;

namespace FumoQuake
{
    public class Fodder
    {
        [System.Serializable]
        public class ProjectileShotgun : BaseGun, IQuakeShooter
        {
            [System.Serializable]
            public struct ShotgunData
            {
                public int projectileIndex;
                public int pellets;
                [SerializeField] List<float> pelletRandomDamage;
                public float projectileSpeed;
                public float RandomDamage => pelletRandomDamage.RandomResult() is float f && f > 0f ? f : 10f;
            }
            public ShotgunData shotgun;
            public override bool IsProjectileWeapon => true;
            public void Shoot(WeaponsController runner, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                GunSound.Play(ray.origin);
                for (int i = 0; i < shotgun.pellets.Max(1); i++)
                {
                    Ray r = RinHelper.RayDot(ray, 0.03f);
                    QuakeProjectile.CreateProjectile(new() { Direction = r.direction, Faction = OwnerFaction, Origin = r.origin, Speed = shotgun.projectileSpeed },
                        out QuakeProjectile p);

                    if (p != null)
                    {
                        p.Channel = shotgun.projectileIndex;
                        p.Damage = shotgun.RandomDamage;
                    }
                }
                SetNewLockTimes(ref weaponLock);
            }
        }
        [System.Serializable]
        public class ProjectileLinegun : BaseGun, IQuakeShooter
        {
            [System.Serializable]
            public struct LinegunData
            {
                public int projectileIndex;
                public int pellets;
                [SerializeField] List<float> pelletRandomDamage;
                public float baseProjectileSpeed;
                public float maxProjectileSpeed;
                public float RandomDamage => pelletRandomDamage.RandomResult() is float f && f > 0f ? f : 10f;
            }
            public LinegunData linegun;
            public override bool IsProjectileWeapon => true;
            public void Shoot(WeaponsController runner, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                float diffProjectileSpeed = (linegun.maxProjectileSpeed - linegun.baseProjectileSpeed) / linegun.pellets;
                GunSound.Play(ray.origin);
                Ray r = RinHelper.RayDot(ray, 0.00f);
                for (int i = 0; i < linegun.pellets.Max(1); i++)
                {
                    QuakeProjectile.CreateProjectile(new() { Direction = r.direction, Faction = OwnerFaction, Origin = r.origin, Speed = linegun.baseProjectileSpeed + i * diffProjectileSpeed },
                        out QuakeProjectile p);

                    if (p != null)
                    {
                        p.Channel = linegun.projectileIndex;
                        p.Damage = linegun.RandomDamage;
                    }
                }
                SetNewLockTimes(ref weaponLock);
            }
        }
    }
}
