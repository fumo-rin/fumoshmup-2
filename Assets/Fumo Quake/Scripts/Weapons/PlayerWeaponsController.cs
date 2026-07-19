using rinCore;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using System.Collections;
using UnityEngine.SocialPlatforms;
using Mono.CSharp.Linq;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using UnityEditor;
using UnityEngine.UIElements;
using WebSocketSharp;

namespace FumoQuake
{
    #region Recoil
    public partial class PlayerWeaponsController
    {
        [SerializeField] QuakeDude recoilTarget;
        [SerializeField] float maxRecoil = 55f;
        public void AddRecoil(float amount)
        {
            recoilTarget.AddRecoil(amount, maxRecoil);
        }
    }
    #endregion
    public partial class PlayerWeaponsController : WeaponsController
    {
        [SerializeField] List<InputActionReference> orderedSelectBinds = new();
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun1 { get; protected set; }
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun2 { get; protected set; }
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun3 { get; protected set; }
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun4 { get; protected set; }
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun5 { get; protected set; }
        [field: SerializeReference, ManagedReferencePicker] public BaseGun Gun6 { get; protected set; }
        [SerializeField] List<GameObject> gunModelNests = new();
        BaseGun forSelected_PreviousWeapon;
        public static bool UnlockPickup(int index)
        {
            var item = currentLoadout[index % currentLoadout.Count()];
            if (item == null)
                return false;
            if (!item.IsLocked)
            {
                if (AwardAmmo(index, 0.2f, out float delta) && delta >= 1f)
                {
                    QuakeTextInfoUI.AddText("You Gots " + delta.ToString("F0") + (item is IQuakeTextName n2 ? n2.TextName + " ammo" : "Item"));
                    return true;
                }
                return false;
            }
            QuakeTextInfoUI.AddText("You Gots " + (item is IQuakeTextName n ? n.TextName : "Item"));
            item.IsLocked = false;
            return true;
        }
        public static bool AwardAmmo(int index, float amount01, out float delta)
        {
            var item = currentLoadout[index % currentLoadout.Count()];
            delta = 0f;
            if (item is IGunAmmo ammo)
            {
                float prev = ammo.RemainingAmmo;
                ammo.RemainingAmmo += ammo.MaxAmmo.AsFloat(amount01).Floor().ToInt();
                ammo.RemainingAmmo = ammo.RemainingAmmo.Clamp(0, ammo.MaxAmmo);
                delta = ammo.RemainingAmmo - prev;
                return true;
            }
            return false;
        }
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
                    if (item == null) continue;
                    int index = GetWeaponIndex(item);
                    yield return new(index, item);
                }
            }
        }
        [Initialize(10)]
        public static void ResetWeaponState()
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

                    currentLoadout.Add(item.Clone());
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
                    update.Update(this, Time.deltaTime);
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
                weaponLockTiming.NextShootTime = Time.time + .125f;
            }
        }
        Coroutine runningSwap = null;
        private void SwapToWeapon(BaseGun targetWeapon)
        {
            forSelected_PreviousWeapon = CurrentWeapon;

            weaponLockTiming.WeaponSwapLockTime = weaponLockTiming.WeaponSwapLockTime.Max(Time.time + 0.125f);
            weaponLockTiming.NextShootTime = weaponLockTiming.NextShootTime.Max(Time.time + 0.125f);
            CurrentWeapon = targetWeapon;

            bool swapped = forSelected_PreviousWeapon != CurrentWeapon;
            forSelected_PreviousWeapon = CurrentWeapon;
            if (swapped)
            {
                int index = GetWeaponIndex(targetWeapon);
                WhenWeaponSelection?.Invoke(index, targetWeapon);
                if (targetWeapon != null && targetWeapon is IQuakeTextName n)
                {
                    GameXYTextDisplay.CreateText(n.TextName, new()
                    {
                        a01 = new(0.2f, 0.1f),
                        b01 = new(0.8f, 0.2f),
                        color = ColorHelper.White,
                        duration = 1.25f,
                        fadeIn = 0f,
                        fadeOut = 0.1f,
                        fontSize = 22f,
                        horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center,
                        verticalAlignment = TMPro.VerticalAlignmentOptions.Bottom
                    }, "Player Weapon Selection");
                }


                IEnumerator CO_Swap(int index)
                {
                    Vector3 topPos = Vector3.zero;
                    Vector3 botPos = new Vector3(0f, -0.2f, 0f);

                    Quaternion topRot = Quaternion.identity;
                    Quaternion botRot = Quaternion.Euler(30f, 0f, 20f);

                    float halfTime = 0.07f;
                    float elapsed = 0f;

                    while (elapsed < halfTime)
                    {
                        float t = Mathf.SmoothStep(0f, 1f, elapsed / halfTime);
                        for (int i = 0; i < gunModelNests.Count; i++)
                        {
                            var item = gunModelNests[i];
                            if (item == null) continue;

                            item.transform.localPosition = Vector3.Lerp(topPos, botPos, t);
                            item.transform.localRotation = Quaternion.Slerp(topRot, botRot, t);
                        }
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    for (int i = 0; i < gunModelNests.Count; i++)
                    {
                        if (gunModelNests[i] != null)
                            gunModelNests[i].SetActive(i == index);
                    }
                    elapsed = 0f;
                    while (elapsed < halfTime)
                    {
                        float t = Mathf.SmoothStep(0f, 1f, elapsed / halfTime);
                        for (int i = 0; i < gunModelNests.Count; i++)
                        {
                            var item = gunModelNests[i];
                            if (item == null) continue;

                            item.transform.localPosition = Vector3.Lerp(botPos, topPos, t);
                            item.transform.localRotation = Quaternion.Slerp(botRot, topRot, t);
                        }
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    for (int i = 0; i < gunModelNests.Count; i++)
                    {
                        if (gunModelNests[i] == null) continue;
                        gunModelNests[i].transform.localPosition = topPos;
                        gunModelNests[i].transform.localRotation = topRot;
                    }
                }

                if (runningSwap != null)
                {
                    StopCoroutine(runningSwap);
                }
                runningSwap = StartCoroutine(CO_Swap(index));
            }
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