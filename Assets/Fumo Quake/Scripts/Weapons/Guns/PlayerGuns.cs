using UnityEngine;
using UnityEngine.InputSystem;
using rinCore;
using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;
namespace FumoQuake
{
    public partial class PlayerGuns
    {
        [System.Serializable]
        public class PlayerShotgun : BaseGun, IQuakeShooter
        {
            [System.Serializable]
            public struct ShotgunData
            {
                public int pellets;
                public float damageTotal;
            }
            public ShotgunData shotgun;
            [SerializeField] LayerMask hitMask;
            [SerializeField] float hitDistance;
            public override bool IsProjectileWeapon => false;
            public void Shoot(ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                GunSound.Play(ray.origin);
                float pelletDamage = shotgun.damageTotal / shotgun.pellets.Max(1);
                for (int i = 0; i < shotgun.pellets.Max(1); i++)
                {
                    Ray r = RinHelper.RayDot(ray, 0.0065f);

                    if (new RinRaycast(r, hitMask, hitDistance, QueryTriggerInteraction.Ignore)
                        .With(HitEffect)
                        .With(Knockback)
                        .Cast(out RaycastHit hit, out IQuakeHitable hitable, pelletDamage, true))
                    {
                        if (IFumoUnit.Player.unitGameObject is GameObject g && hitable.hitGameObject != g)
                        {
                            Mugshot.SetMood(new(1.65f)
                            {
                                mood = Mugshot.Mood.Excited,
                                priority = 50,
                            });
                        }
                        hitable.Hit(new()
                        {
                            Damage = pelletDamage,
                            HitPoint = hit.point
                        });
                    }
                }
                SetNewLockTimes(ref weaponLock);
            }
        }
        [System.Serializable]
        public class NailGun : BaseGun, IQuakeShooter
        {
            [System.Serializable]
            public struct NailGunData
            {
                public int projectileIndex;
                public float pelletDamage;
            }
            public NailGunData data;
            public override bool IsProjectileWeapon => true;
            public void Shoot(ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                GunSound.Play(ray.origin);
                Ray r = RinHelper.RayDot(ray, 0.0005f);
                QuakeProjectile.CreateProjectile(new() { Direction = r.direction, Faction = OwnerFaction, Origin = r.origin, Speed = 72f },
                    out QuakeProjectile p);

                if (p != null)
                {
                    p.Channel = data.projectileIndex;
                    p.Damage = data.pelletDamage;
                    p.GravityMod = 0.5f;
                }
                SetNewLockTimes(ref weaponLock);
            }
        }
    }
}
