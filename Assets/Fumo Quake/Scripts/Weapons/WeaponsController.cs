using UnityEngine;
using UnityEngine.InputSystem;
using rinCore;
using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FumoQuake
{
    public interface IGunAmmo
    {
        public int RemainingAmmo { get; set; }
        public int MaxAmmo { get; }
        public int StartingAmmo { get; }
    }
    public interface IQuakeShooter
    {
        public struct WeaponLock
        {
            public float NextShootTime;
            public float WeaponSwapLockTime;
            public bool CanShoot => Time.time >= NextShootTime;
            public bool CanSwapWeapon => Time.time >= WeaponSwapLockTime;
        }
        public void Shoot(ref WeaponLock weaponLock, Ray ray);
    }
    public abstract class BaseGun
    {
        #region Hitscan Actions
        protected void Knockback(RinRaycast cast, RaycastHit hit, float damage)
        {
            hit.collider.AddImpactVelocity(new Impact(hit, cast.ray, damage * 0.1f));
        }
        protected void HitEffect(RinRaycast cast, RaycastHit hit, float damage)
        {
            QuakeProjectileRenderer.ProjHitEffect(hit);
        }

        #endregion
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
    }
    public class WeaponsController : MonoBehaviour
    {
        [field: SerializeReference, ManagedReferencePicker] public BaseGun CurrentWeapon { get; protected set; }
        public bool IsProjectileWeapon => CurrentWeapon == null ? false : CurrentWeapon.IsProjectileWeapon;
        public void AssignWeapon(BaseGun g)
        {
            CurrentWeapon = g;
        }
        public bool ValidWeapon => CurrentWeapon != null;
        public IQuakeShooter.WeaponLock weaponLockTiming = new();
        public float RandomAggressionTimeAfterLock(float min, float max) => weaponLockTiming.NextShootTime + RNG.FloatRange(min, max);
        public bool TryShootWith(BaseGun item, Ray r)
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
                gun.Shoot(ref weaponLockTiming, r);
            }
            return true;
        }
        public bool TryShootWith(Ray r)
        {
            return TryShootWith(CurrentWeapon, r);
        }
    }
}
