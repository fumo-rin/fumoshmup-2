using rinCore;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

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
        static List<BaseGun> currentLoadout;
        static bool ShouldInitialize;
        int queuedSelection = -1;
        float clickTime;

        private void Awake()
        {
            if (ShouldInitialize || currentLoadout == null)
            {
                currentLoadout = new();
                List<BaseGun> loadout = new();
                foreach (var item in startingLoadout)
                {
                    if (item == null) continue;

                    string json = JsonUtility.ToJson(item);
                    BaseGun clonedGun = (BaseGun)JsonUtility.FromJson(json, item.GetType());

                    currentLoadout.Add(clonedGun);
                }
            }
        }

        private void Update()
        {
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
                queuedSelection = -1;
                weaponLockTiming.WeaponSwapLockTime = Time.time + 0.125f;
                weaponLockTiming.NextShootTime = Time.time + 0.125f;
                CurrentWeapon = selectedGun;
            }
        }
        bool CanSelect(int value, out BaseGun selection)
        {
            selection = null;
            bool selectionValid = false;
            if (startingLoadout.ToList().TryGetIndex(value, out selection))
            {
                if (selection is not IGunAmmo g)
                {
                    selectionValid = true;
                }
                else
                {
                    selectionValid = g.RemainingAmmo > 0;
                }
            }
            return selectionValid;
        }
    }
}
