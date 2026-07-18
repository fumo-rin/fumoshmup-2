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
    }
}
