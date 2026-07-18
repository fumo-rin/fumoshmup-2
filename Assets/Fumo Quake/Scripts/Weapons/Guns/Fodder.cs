using UnityEngine;
using rinCore;

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
                public float damageTotal;
                public float projectileSpeed;
            }
            public ShotgunData shotgun;
            public override bool IsProjectileWeapon => true;
            public void Shoot(MonoBehaviour runner, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
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
                        p.Damage = shotgun.damageTotal / shotgun.pellets.Max(1);
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
                public float damageTotal;
                public float baseProjectileSpeed;
                public float maxProjectileSpeed;
            }
            public LinegunData linegun;
            public override bool IsProjectileWeapon => true;
            public void Shoot(MonoBehaviour runner, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
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
                        p.Damage = linegun.damageTotal / linegun.pellets.Max(1);
                    }
                }
                SetNewLockTimes(ref weaponLock);
            }
        }
    }
}
