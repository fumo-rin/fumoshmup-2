using UnityEngine;
using UnityEngine.InputSystem;
using rinCore;
using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;
namespace FumoQuake
{
    public interface IGunUpdate
    {
        public void Update(float dt);
    }
    public interface IGunFireMode
    {
        public enum Mode
        {
            Click,
            Hold,
            Charge
        }
        public Mode ClickMode { get; }
    }
    public partial class PlayerGuns
    {
        [System.Serializable]
        public class GunAmmo
        {
            public int Remaining;
            public int MaxAmmo;
            public void GiveAmmo(int count)
            {
                Remaining += count;
                Remaining = Remaining.Clamp(0, MaxAmmo);
            }
        }
        [System.Serializable]
        public class PlayerShotgun : BaseGun, IQuakeShooter, IGunAmmo, IGunFireMode
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

            [SerializeField]
            GunAmmo gunAmmo = new()
            {
                MaxAmmo = 100,
                Remaining = 40
            };
            public int RemainingAmmo
            {
                get
                {
                    return gunAmmo.Remaining;
                }
                set
                {
                    gunAmmo.Remaining = value;
                }
            }
            public int MaxAmmo => gunAmmo.MaxAmmo;
            public int StartingAmmo => 40;
            public int AmmoCost => 1;
            public IGunFireMode.Mode ClickMode => IGunFireMode.Mode.Click;
            public string TextName => "Shotgun";
            public void Shoot(WeaponsController runner, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                if (runner.GetRecoilHandler(out PlayerWeaponsController recoil))
                {
                    recoil.AddRecoil(18f);
                }
                GunSound.Play(ray.origin);
                float pelletDamage = shotgun.damageTotal / shotgun.pellets.Max(1);
                for (int i = 0; i < shotgun.pellets.Max(1); i++)
                {
                    Ray r = RinHelper.RayDot(ray, 0.0045f);

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
                            HitPoint = hit.point,
                            Sender = runner.GetComponent<ITargetting>() is ITargetting sender ? sender : null,
                        });
                    }
                }
                SetNewLockTimes(ref weaponLock);
            }

            public override BaseGun Clone()
            {
                return new PlayerShotgun()
                {
                    WeaponShootLockTime = WeaponShootLockTime,
                    gunAmmo = gunAmmo,
                    GunSound = GunSound,
                    hitDistance = hitDistance,
                    hitMask = hitMask,
                    IsLocked = IsLocked,
                    optionalIconUI = optionalIconUI,
                    OwnerFaction = OwnerFaction,
                    RemainingAmmo = RemainingAmmo,
                    shotgun = shotgun,
                    WeaponShootSwapLockDuration = WeaponShootSwapLockDuration
                };
            }
        }

        [System.Serializable]
        public class PlayerPistol : BaseGun, IQuakeShooter, IGunAmmo, IGunFireMode, IGunUpdate
        {
            [System.Serializable]
            public struct GunData
            {
                public int projectileIndex;
                public float pelletDamage;
                public int pelletCount;
            }
            public override bool IsProjectileWeapon => true;
            private float LastShootTime;
            private float nextAmmoGenTime;
            private bool IsRegenningAmmo => Time.time > LastShootTime + 2f;
            private bool AmmoTick => IsRegenningAmmo && (Time.time >= nextAmmoGenTime);
            [SerializeField] GunData data;
            [SerializeField]
            GunAmmo gunAmmo = new()
            {
                MaxAmmo = 5,
                Remaining = 5
            };
            public int RemainingAmmo
            {
                get
                {
                    return gunAmmo.Remaining;
                }
                set
                {
                    gunAmmo.Remaining = value;
                }
            }
            public int MaxAmmo => gunAmmo.MaxAmmo;
            public int StartingAmmo => 5;
            public int AmmoCost => 1;
            public IGunFireMode.Mode ClickMode => IGunFireMode.Mode.Click;
            public string TextName => "Pistol";
            public void Shoot(WeaponsController runner, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                if (runner.GetRecoilHandler(out PlayerWeaponsController recoil))
                {
                    recoil.AddRecoil(1.9f);
                }
                SetNewLockTimes(ref weaponLock);
                GunSound.Play(ray.origin);
                LastShootTime = Time.time;
                for (int i = 0; i < data.pelletCount; i++)
                {
                    Ray r = RinHelper.RayDot(ray, 0.0003f);
                    QuakeProjectile.CreateProjectile(new() { Direction = r.direction, Faction = OwnerFaction, Origin = r.origin, Speed = 25f },
                        out QuakeProjectile p);

                    if (p != null)
                    {
                        p.Channel = data.projectileIndex;
                        p.Damage = data.pelletDamage;
                        p.GravityMod = 0f;
                        p.Sender = runner.GetComponent<ITargetting>() is ITargetting sender ? sender : null;
                    }
                }
            }
            public void Update(float dt)
            {
                if (AmmoTick)
                {
                    gunAmmo.GiveAmmo(1);
                    nextAmmoGenTime = Time.time + 0.125f;
                }
            }

            public override BaseGun Clone()
            {
                return new PlayerPistol()
                {
                    WeaponShootLockTime = WeaponShootLockTime,
                    gunAmmo = gunAmmo,
                    GunSound = GunSound,
                    nextAmmoGenTime = nextAmmoGenTime,
                    LastShootTime = LastShootTime,
                    data = data,
                    IsLocked = IsLocked,
                    optionalIconUI = optionalIconUI,
                    OwnerFaction = OwnerFaction,
                    RemainingAmmo = RemainingAmmo,
                    WeaponShootSwapLockDuration = WeaponShootSwapLockDuration
                };
            }
        }
        [System.Serializable]
        public class NailGun : BaseGun, IQuakeShooter, IGunAmmo, IGunFireMode, IQuakeTextName
        {
            [System.Serializable]
            public struct NailGunData
            {
                public int projectileIndex;
                public float pelletDamage;
            }
            public NailGunData data;
            [SerializeField]
            GunAmmo gunAmmo = new()
            {
                MaxAmmo = 200,
                Remaining = 80
            };
            public int RemainingAmmo
            {
                get
                {
                    return gunAmmo.Remaining;
                }
                set
                {
                    gunAmmo.Remaining = value;
                }
            }
            public int MaxAmmo => gunAmmo.MaxAmmo;
            public int StartingAmmo => 80;
            public int AmmoCost => 1;
            public override bool IsProjectileWeapon => true;
            public IGunFireMode.Mode ClickMode => IGunFireMode.Mode.Hold;

            public string TextName => "Nail Gun";

            public void Shoot(WeaponsController runner, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                if (runner.GetRecoilHandler(out PlayerWeaponsController recoil))
                {
                    recoil.AddRecoil(2.25f);
                }
                GunSound.Play(ray.origin);
                Ray r = RinHelper.RayDot(ray, 0.00035f);
                QuakeProjectile.CreateProjectile(new() { Direction = r.direction, Faction = OwnerFaction, Origin = r.origin, Speed = 72f },
                    out QuakeProjectile p);

                if (p != null)
                {
                    p.Channel = data.projectileIndex;
                    p.Damage = data.pelletDamage;
                    p.GravityMod = 0.5f;
                    p.Sender = runner.GetComponent<ITargetting>() is ITargetting sender ? sender : null;
                }
                SetNewLockTimes(ref weaponLock);
            }

            public override BaseGun Clone() => new NailGun()
            {
                WeaponShootLockTime = WeaponShootLockTime,
                data = data,
                gunAmmo = gunAmmo,
                GunSound = GunSound,
                IsLocked = IsLocked,
                optionalIconUI = optionalIconUI,
                OwnerFaction = OwnerFaction,
                RemainingAmmo = RemainingAmmo,
                WeaponShootSwapLockDuration = WeaponShootSwapLockDuration,
            };
        }
    }
}
