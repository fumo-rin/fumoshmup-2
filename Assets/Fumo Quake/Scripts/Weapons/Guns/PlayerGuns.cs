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
        public void Update(WeaponsController runner, float dt);
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
        public class PlayerShotgun : BaseGun, IQuakeShooter, IGunAmmo, IGunFireMode, IQuakeTextName
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
            public string TextName => "Shotguns";
            public void Shoot(WeaponsController runner, ITargetting sender, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
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
                        hitable.Hit(new(sender)
                        {
                            Damage = pelletDamage,
                            HitPoint = hit.point,
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
        public class PlayerPistol : BaseGun, IQuakeShooter, IGunAmmo, IGunFireMode, IGunUpdate, IQuakeTextName
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
            public void Shoot(WeaponsController runner, ITargetting Sender, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
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
                        Sender, out QuakeProjectile p);

                    if (p != null)
                    {
                        p.Channel = data.projectileIndex;
                        p.Damage = data.pelletDamage;
                        p.GravityMod = 0f;
                        p.HitAction += (QuakeProjectile.HitActionResult hitResult) =>
                        {
                            if (hitResult == QuakeProjectile.HitActionResult.IHit)
                                Mugshot.SetMood(new(0.5f)
                                {
                                    mood = Mugshot.Mood.Excited,
                                    priority = 50,
                                });
                        };
                    }
                }
            }
            public void Update(WeaponsController runner, float dt)
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

            public string TextName => "Nails Gun";

            public void Shoot(WeaponsController runner, ITargetting sender, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                if (runner.GetRecoilHandler(out PlayerWeaponsController recoil))
                {
                    recoil.AddRecoil(2.25f);
                }
                GunSound.Play(ray.origin);
                Ray r = RinHelper.RayDot(ray, 0.00035f);
                QuakeProjectile.CreateProjectile(new() { Direction = r.direction, Faction = OwnerFaction, Origin = r.origin, Speed = 72f }
                    , sender, out QuakeProjectile p);

                if (p != null)
                {
                    p.Channel = data.projectileIndex;
                    p.Damage = data.pelletDamage;
                    p.GravityMod = 0.5f;
                    p.HitAction += (QuakeProjectile.HitActionResult hitResult) =>
                    {
                        if (hitResult == QuakeProjectile.HitActionResult.IHit)
                            Mugshot.SetMood(new(0.5f)
                            {
                                mood = Mugshot.Mood.Excited,
                                priority = 50,
                            });
                    };
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
        [System.Serializable]
        public class RocketLauncher : BaseGun, IQuakeShooter, IGunAmmo, IGunFireMode, IQuakeTextName
        {
            public override bool IsProjectileWeapon => true;
            public GunAmmo gunAmmo;
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
            [System.Serializable]
            public struct GunData
            {
                public int projectileIndex;
                public float projectileSpeed;
                public float rocketDamage;
                public float splashSize;
            }
            public GunData data;
            public int MaxAmmo => 30;
            public int StartingAmmo => 12;
            public int AmmoCost => 1;
            public IGunFireMode.Mode ClickMode => IGunFireMode.Mode.Hold;
            public string TextName => "Rockets Gun";
            public override BaseGun Clone()
            {
                return new RocketLauncher()
                {
                    WeaponShootLockTime = WeaponShootLockTime,
                    gunAmmo = gunAmmo,
                    GunSound = GunSound,
                    IsLocked = IsLocked,
                    optionalIconUI = optionalIconUI,
                    OwnerFaction = OwnerFaction,
                    RemainingAmmo = RemainingAmmo,
                    WeaponShootSwapLockDuration = WeaponShootSwapLockDuration,
                    data = data,
                };
            }
            public void Shoot(WeaponsController runner, ITargetting sender, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                if (runner.GetRecoilHandler(out PlayerWeaponsController recoil))
                {
                    recoil.AddRecoil(11f);
                }
                SetNewLockTimes(ref weaponLock);
                GunSound.Play(ray.origin);

                Ray r = RinHelper.RayDot(ray, 0.0003f);
                QuakeProjectile.CreateProjectile(new() { Direction = r.direction, Faction = OwnerFaction, Origin = r.origin, Speed = 26f },
                    sender, out QuakeProjectile p);

                if (p != null)
                {
                    p._projectileVelocity += new Vector3(0f, 3f, 0f);
                    p.Channel = data.projectileIndex;
                    p.Damage = data.rocketDamage;
                    p.GravityMod = 0.65f;
                    p.HitAction += (QuakeProjectile.HitActionResult hitResult) =>
                    {
                        if (hitResult == QuakeProjectile.HitActionResult.IHit)
                            Mugshot.SetMood(new(0.5f)
                            {
                                mood = Mugshot.Mood.Excited,
                                priority = 50,
                            });

                        GeneralManager.FunnyExplosion(new()
                        {
                            is3d = true,
                            playSound = true,
                            position = p.FinalizedPosition,
                            scale = 1.75f
                        });
                        bool hitSomething = false;
                        foreach (var item in Physics.OverlapSphere(p.FinalizedPosition, data.splashSize))
                        {
                            Vector3 point = item.ClosestPointOnBounds(p.FinalizedPosition);
                            float lerp01 = p.FinalizedPosition.DistanceTo(point).MapTo01(4f, 0f);

                            if (item.transform.GetComponent<Collider>() is Collider c)
                            {
                                c.AddImpactVelocity(new(point, (point - p.FinalizedPosition), 7f + (4f * lerp01)));
                            }

                            if (item.transform.GetComponentInParent<IQuakeHitable>() is not IQuakeHitable hit)
                                continue;
                            hitSomething = true;
                            float damage = data.rocketDamage * 0.5f + (0f.LerpUnclamped(data.rocketDamage, lerp01));
                            if (hit.IsPlayer)
                                damage = 4f + 0f.LerpUnclamped(14f, lerp01);
                            hit.Hit(new(sender)
                            {
                                Damage = damage,
                                HitPoint = item.ClosestPoint(p.FinalizedPosition),
                            });
                        }
                        if (hitSomething)
                        {
                            Mugshot.SetMood(new Mugshot.MoodEntry(1.35f)
                            {
                                mood = Mugshot.Mood.Excited,
                                priority = 50,
                            });
                        }
                    };
                }
            }
        }
        [System.Serializable]
        public class LightningGun : BaseGun, IQuakeShooter, IGunAmmo, IGunFireMode, IQuakeTextName, IGunUpdate
        {
            [System.Serializable]
            public struct LG
            {
                public AudioSource LGSound;
                public InputActionReference shootAction;
                public LineRenderer lr;
                public float DPS;
                public LayerMask hitLayer;
            }
            public LG data;
            [SerializeField]
            GunAmmo gunAmmo = new()
            {
                MaxAmmo = 600,
                Remaining = 600
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
            public int StartingAmmo => 600;
            public int AmmoCost => 1;
            public override bool IsProjectileWeapon => false;
            public IGunFireMode.Mode ClickMode => IGunFireMode.Mode.Hold;
            public string TextName => "Gay Laser from Hell";
            public void Shoot(WeaponsController runner, ITargetting sender, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                if (!data.LGSound.isPlaying) data.LGSound.Play();
                data.LGSound.loop = true;
                Ray r = ray;
                Vector3 endPoint = r.origin + r.direction.ScaleToMagnitude(35f);
                SetNewLockTimes(ref weaponLock);
                if (Physics.Raycast(r, out RaycastHit hit, 35f, data.hitLayer, QueryTriggerInteraction.Ignore))
                {
                    endPoint = hit.point;
                    QuakeProjectileRenderer.ProjHitEffect(hit);
                    if (hit.transform.TryGetComponent(out IQuakeHitable hitable))
                    {
                        hitable.Hit(new(sender)
                        {
                            Damage = data.DPS * WeaponShootLockTime,
                            HitPoint = hit.point,
                        });

                        Mugshot.SetMood(new Mugshot.MoodEntry(0.35f)
                        {
                            mood = Mugshot.Mood.Excited,
                            priority = 50,
                        });
                    }
                    if (hit.collider is Collider c) c.AddImpactVelocity(new Impact(hit, r, 0.45f));

                }
                ray.origin = ray.origin + new Vector3(0f, -0.5f, 0f);
                data.lr.positionCount = 2;
                data.lr.SetPositions(new[] { ray.origin, endPoint });
                data.lr.enabled = true;
            }

            public override BaseGun Clone() => new LightningGun()
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
            public void Update(WeaponsController runner, float dt)
            {
                if (!data.shootAction.IsPressed() || RemainingAmmo <= 0f || runner.CurrentWeapon != this)
                {
                    data.LGSound.Stop();
                    data.lr.positionCount = 0;
                    data.lr.enabled = false;
                }
            }
        }
        [System.Serializable]
        public class RailGun : BaseGun, IQuakeShooter, IGunAmmo, IGunFireMode, IQuakeTextName
        {
            [System.Serializable]
            public struct Data
            {
                public ParticleSystem railgunPs;
                public float ShotDamage;
                public LayerMask hitLayer;
            }
            public Data data;
            [SerializeField]
            GunAmmo gunAmmo = new()
            {
                MaxAmmo = 20,
                Remaining = 8
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
            public int StartingAmmo => 8;
            public int AmmoCost => 1;
            public override bool IsProjectileWeapon => false;
            public IGunFireMode.Mode ClickMode => IGunFireMode.Mode.Click;
            public string TextName => "Railgun";
            public void Shoot(WeaponsController runner, ITargetting sender, ref IQuakeShooter.WeaponLock weaponLock, Ray ray)
            {
                GunSound.Play(ray.origin);
                if (runner.GetRecoilHandler(out PlayerWeaponsController recoil))
                {
                    recoil.AddRecoil(32f);
                }
                Ray r = ray;
                Vector3 endPoint = r.origin + r.direction.ScaleToMagnitude(50f);
                SetNewLockTimes(ref weaponLock);

                if (new RinRaycast(r, data.hitLayer, 50f, QueryTriggerInteraction.Ignore)
                    .With(HitEffect)
                    .With(SuperKnockback)
                    .Cast(out RaycastHit hit, out IQuakeHitable hitable, data.ShotDamage, true))
                {
                    if (IFumoUnit.Player.unitGameObject is GameObject g && hitable.hitGameObject != g)
                    {
                        Mugshot.SetMood(new(1.65f)
                        {
                            mood = Mugshot.Mood.Excited,
                            priority = 50,
                        });
                    }
                    hitable.Hit(new(sender)
                    {
                        Damage = data.ShotDamage,
                        HitPoint = hit.point,
                    });
                }
                ray.origin = ray.origin + new Vector3(0f, -0.5f, 0f);
                IEnumerable<Vector3> line = RinHelper.RayChop.Chop(ray, 0.4f, 75f);
                foreach (var item in line)
                {
                    ParticleSystem ps = GameObject.Instantiate(data.railgunPs, item, Quaternion.identity);
                    ps.Play();
                }
            }

            public override BaseGun Clone() => new RailGun()
            {
                WeaponShootLockTime = WeaponShootLockTime,
                data = data,
                IsLocked = IsLocked,
                optionalIconUI = optionalIconUI,
                OwnerFaction = OwnerFaction,
                RemainingAmmo = RemainingAmmo,
                WeaponShootSwapLockDuration = WeaponShootSwapLockDuration,
                gunAmmo = gunAmmo,
                GunSound = GunSound
            };
        }
    }
}
