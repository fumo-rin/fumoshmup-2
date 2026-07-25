using UnityEngine;
using UnityEngine.InputSystem;
using rinCore;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UIElements;


#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FumoQuake
{
    public interface IGunAmmo
    {
        public int RemainingAmmo { get; set; }
        public float AmmoPercent01 => RemainingAmmo.AsFloat() / MaxAmmo.AsFloat();
        public int MaxAmmo { get; }
        public int StartingAmmo { get; }
        public int AmmoCost { get; }
        public int SpendAmmo()
        {
            RemainingAmmo -= AmmoCost;
            return RemainingAmmo;
        }
        public bool HasAmmo => RemainingAmmo > 0;
    }
    public interface IQuakeShooter
    {
        public struct WeaponLock
        {
            public float NextShootTime;
            public float WeaponSwapLockTime;
            public bool CanShoot => Time.time >= NextShootTime;
            public bool CanSwapWeapon => Time.time >= WeaponSwapLockTime;
            public void Stall(float time)
            {
                NextShootTime = (time + Time.time).Max(NextShootTime);
                WeaponSwapLockTime = (time + Time.time).Max(WeaponSwapLockTime);
            }
        }
        public void Shoot(WeaponsController runner, ITargetting sender, ref WeaponLock weaponLock, Ray ray);
    }
    public abstract class BaseGun
    {
        public abstract BaseGun Clone();
        #region Hitscan Actions
        protected void Knockback(RinRaycast cast, RaycastHit hit, float damage)
        {
            float force = damage * 0.125f;
            hit.collider.AddImpactVelocity(new Impact(hit, cast.ray, force.Clamp(0.125f, 18f)));
        }
        protected void SuperKnockback(RinRaycast cast, RaycastHit hit, float damage)
        {
            float force = damage * 0.125f;
            hit.collider.AddImpactVelocity(new Impact(hit, cast.ray, force.Clamp(0.125f, 40f)));
        }
        protected void HitEffect(RinRaycast cast, RaycastHit hit, float damage)
        {
            QuakeProjectileRenderer.ProjHitEffect(hit);
        }

        #endregion
        [SerializeField] public Sprite optionalIconUI;
        [SerializeField] protected ACWrapper GunSound;
        public abstract bool IsProjectileWeapon { get; }
        [field: SerializeField] public QuakeFaction OwnerFaction { get; protected set; } = QuakeFaction.Player;
        [Range(0.02f, 3f)] public float WeaponShootLockTime = 0.08f;
        [Range(0.02f, 3f)] public float WeaponShootSwapLockDuration = 0.05f;
        public void SetNewLockTimes(ref IQuakeShooter.WeaponLock weaponLock)
        {
            weaponLock.NextShootTime = weaponLock.NextShootTime.Max(Time.time + WeaponShootLockTime);
            weaponLock.WeaponSwapLockTime = weaponLock.WeaponSwapLockTime.Max(Time.time + WeaponShootSwapLockDuration);
        }
        public bool IsLocked = false;
    }
    public class WeaponsController : MonoBehaviour
    {
        public bool GetRecoilHandler(out PlayerWeaponsController recoil)
        {
            recoil = null;
            if (this is PlayerWeaponsController pwc)
                recoil = pwc;
            return recoil != null;
        }
        [field: SerializeReference, ManagedReferencePicker] public BaseGun CurrentWeapon { get; protected set; }
        public bool IsProjectileWeapon => CurrentWeapon == null ? false : CurrentWeapon.IsProjectileWeapon;
        public void AssignWeapon(BaseGun g)
        {
            CurrentWeapon = g;
        }
        public bool ValidWeapon => CurrentWeapon != null;
        public IQuakeShooter.WeaponLock weaponLockTiming = new();
        public float RandomAggressionTimeAfterLock(float min, float max) => weaponLockTiming.NextShootTime + RNG.FloatRange(min, max);
        public bool TryShootWith(BaseGun item, ITargetting sender, Ray r)
        {
            if (item == null || !ValidWeapon)
            {
                return false;
            }
            if (!weaponLockTiming.CanShoot)
            {
                return false;
            }
            if (item is IQuakeShooter gun)
            {
                if (this is PlayerWeaponsController p && gun is IGunAmmo ammo)
                {
                    if (ammo.HasAmmo)
                    {
                        gun.Shoot(this, sender, ref weaponLockTiming, r);
                        ammo.SpendAmmo();
                    }
                    else
                    {
                        if (p.TryGetWeaponWithAmmo(out BaseGun gunWithAmmo))
                        {
                            p.AssignWeapon(gunWithAmmo);
                        }
                    }
                }
                else
                {
                    gun.Shoot(this, sender, ref weaponLockTiming, r);
                }
            }
            return true;
        }
        public bool TryShootWith(ITargetting sender, Ray r)
        {
            return TryShootWith(CurrentWeapon, sender, r);
        }
    }
}
