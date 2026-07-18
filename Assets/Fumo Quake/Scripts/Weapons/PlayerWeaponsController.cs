using rinCore;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using System.Collections;

namespace FumoQuake
{
    public class PlayerWeaponsController : WeaponsController
    {
        [SerializeField] List<InputActionReference> orderedSelectBinds = new();
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun1 { get; protected set; }
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun2 { get; protected set; }
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun3 { get; protected set; }
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun4 { get; protected set; }
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun5 { get; protected set; }
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun6 { get; protected set; }

        IEnumerable<BaseGun> startingLoadout
        {
            get
            {
                yield return Gun1;
                yield return Gun2;
                yield return Gun3;
                yield return Gun4;
                yield return Gun5;
                yield return Gun6;
            }
        }
        IEnumerable<BaseGun> gunsWithAmmo
        {
            get
            {
                foreach (var item in currentLoadout)
                {
                    if (item == null || item.IsLocked) continue;

                    if (item is not IGunAmmo ammo || ammo.RemainingAmmo > 0)
                        yield return item;
                }
            }
        }
        public static IEnumerable<KeyValuePair<int, BaseGun>> LoadoutSnapshot
        {
            get
            {
                foreach (var item in currentLoadout)
                {
                    if (item == null || item.IsLocked) continue;
                    int index = GetWeaponIndex(item);
                    yield return new(index, item);
                }
            }
        }
        [Initialize(10)]
        private static void ResetWeaponState()
        {
            ShouldInitialize = true;
            LastWeaponSelection = 0;
            currentLoadout = null;
        }

        public bool TryGetWeaponWithAmmo(out BaseGun gun)
        {
            gun = gunsWithAmmo.FirstOrDefault();
            return gun != null;
        }

        static List<BaseGun> currentLoadout;
        static bool ShouldInitialize;
        int queuedSelection = -1;
        float clickTime;
        static int LastWeaponSelection = 0;

        public static Action<int, BaseGun> WhenWeaponSelection;
        public static int GetWeaponIndex(BaseGun item)
        {
            for (int i = 0; i < currentLoadout.Count; i++)
            {
                if (item == currentLoadout[i])
                    return i;
            }
            return 0;
        }
        private void Awake()
        {
            if (ShouldInitialize || currentLoadout == null)
            {
                currentLoadout = new();
                foreach (var item in startingLoadout)
                {
                    if (item == null)
                    {
                        currentLoadout.Add(null);
                        continue;
                    }

                    string json = JsonUtility.ToJson(item);
                    BaseGun clonedGun = (BaseGun)JsonUtility.FromJson(json, item.GetType());
                    currentLoadout.Add(clonedGun);
                }
                ShouldInitialize = false;
            }
        }

        private void Start()
        {
            clickTime = Time.time;
            queuedSelection = LastWeaponSelection.Clamp(0, 5);
        }
        private void Update()
        {
            foreach (var item in currentLoadout.Where(x => x != null && !x.IsLocked))
            {
                if (item is IGunUpdate update)
                {
                    update.Update(Time.deltaTime);
                }
            }
            if (CurrentWeapon is IGunAmmo currentAmmo && currentAmmo.RemainingAmmo <= 0)
            {
                if (TryGetWeaponWithAmmo(out BaseGun fallbackGun) && CurrentWeapon != fallbackGun)
                {
                    SwapToWeapon(fallbackGun);
                }
            }

            if (Time.time > clickTime + 1.25f)
                queuedSelection = -1;

            int iteration = 0;
            foreach (var item in orderedSelectBinds)
            {
                if (item.JustPressed())
                {
                    queuedSelection = iteration;
                    clickTime = Time.time;
                    break;
                }
                iteration++;
            }

            bool canSwap = weaponLockTiming.CanSwapWeapon;
            if (canSwap && queuedSelection >= 0 && CanSelect(queuedSelection, out BaseGun selectedGun))
            {
                LastWeaponSelection = queuedSelection;
                queuedSelection = -1;
                SwapToWeapon(selectedGun);
            }
        }

        private void SwapToWeapon(BaseGun targetWeapon)
        {
            weaponLockTiming.WeaponSwapLockTime = weaponLockTiming.WeaponSwapLockTime.Max(Time.time + 0.125f);
            weaponLockTiming.NextShootTime = weaponLockTiming.NextShootTime.Max(Time.time + 0.125f);
            CurrentWeapon = targetWeapon;
            int index = GetWeaponIndex(targetWeapon);
            WhenWeaponSelection?.Invoke(index, targetWeapon);
        }

        bool CanSelect(int value, out BaseGun selection)
        {
            selection = null;
            if (currentLoadout.TryGetIndex(value, out selection) && selection != null)
            {
                if (selection.IsLocked)
                    return false;

                if (selection is IGunAmmo g)
                    return g.RemainingAmmo > 0;

                return true;
            }
            return false;
        }
    }
}