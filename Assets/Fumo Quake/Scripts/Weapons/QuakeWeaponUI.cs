using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using rinCore;

namespace FumoQuake
{
    public class QuakeWeaponUI : MonoBehaviour
    {
        private class WeaponSlot
        {
            public GameObject RootObject;
            public RectTransform Rect;
            public Slider AmmoSlider;

            public int SlotIndex;
            public Vector2 StartSize;
            public Vector2 StartPos;
            public float Progress;
            public int LastAmmo = -1;
            public bool LastLockedState = true;
        }

        private const float UNSELECTED_WIDTH = 60f;
        private const float UNSELECTED_HEIGHT = 60f;
        private const float SELECTED_WIDTH = 120f;
        private const float SELECTED_HEIGHT = 120f;
        private const float SLOT_SPACING = 10f;
        private const float TRANSITION_SPEED = 5f;

        [SerializeField] private GameObject itemTemplate;
        [SerializeField] private RectTransform spawnedItemsNest;

        private readonly List<WeaponSlot> slotsList = new();
        private readonly Dictionary<BaseGun, WeaponSlot> weaponToSlot = new();
        private BaseGun activeGun;

        private void OnEnable() => PlayerWeaponsController.WhenWeaponSelection += OnWeaponSelected;
        private void OnDisable() => PlayerWeaponsController.WhenWeaponSelection -= OnWeaponSelected;

        private void Start()
        {
            if (PlayerWeaponsController.LoadoutSnapshot == null) return;

            int indexCounter = 0;
            foreach (var item in PlayerWeaponsController.LoadoutSnapshot)
            {
                BaseGun gun = item.Value;
                if (gun == null) continue;

                GameObject obj = Instantiate(itemTemplate, spawnedItemsNest);

                var slot = new WeaponSlot
                {
                    RootObject = obj,
                    Rect = obj.GetComponent<RectTransform>(),
                    AmmoSlider = obj.GetComponentInChildren<Slider>(),
                    SlotIndex = indexCounter,
                    StartSize = new Vector2(UNSELECTED_WIDTH, UNSELECTED_HEIGHT),
                    StartPos = Vector2.zero,
                    Progress = 1f,
                    LastLockedState = gun.IsLocked
                };

                if (obj.TryGetComponent(out Image img))
                {
                    img.sprite = gun.optionalIconUI;
                }

                slot.Rect.sizeDelta = slot.StartSize;
                obj.SetActive(!gun.IsLocked);

                slotsList.Add(slot);
                weaponToSlot.Add(gun, slot);
                indexCounter++;
            }

            itemTemplate.SetActive(false);
        }

        private void OnWeaponSelected(int index, BaseGun selectedGun)
        {
            activeGun = selectedGun;

            for (int i = 0; i < slotsList.Count; i++)
            {
                var slot = slotsList[i];
                slot.StartSize = slot.Rect.sizeDelta;
                slot.StartPos = slot.Rect.anchoredPosition;
                slot.Progress = 0f;
            }
        }

        private void Update()
        {
            float accumulatedX = 0f;
            Dictionary<WeaponSlot, Vector2> targetPositions = new();
            Dictionary<WeaponSlot, Vector2> targetSizes = new();

            for (int i = 0; i < slotsList.Count; i++)
            {
                var slot = slotsList[i];
                BaseGun gun = GetGunForSlot(slot);
                if (gun == null) continue;

                if (slot.LastLockedState != gun.IsLocked)
                {
                    slot.LastLockedState = gun.IsLocked;
                    slot.RootObject.SetActive(!gun.IsLocked);

                    if (!gun.IsLocked)
                    {
                        float currentPivotOffset = slot.Rect.pivot.x * UNSELECTED_WIDTH;
                        slot.Rect.anchoredPosition = new Vector2(accumulatedX + currentPivotOffset, slot.Rect.anchoredPosition.y);
                        slot.StartPos = slot.Rect.anchoredPosition;
                    }
                }

                bool isActive = (gun == activeGun);
                float targetW = isActive ? SELECTED_WIDTH : UNSELECTED_WIDTH;
                float targetH = isActive ? SELECTED_HEIGHT : UNSELECTED_HEIGHT;

                float pivotOffsetX = slot.Rect.pivot.x * targetW;
                targetPositions[slot] = new Vector2(accumulatedX + pivotOffsetX, slot.Rect.anchoredPosition.y);
                targetSizes[slot] = new Vector2(targetW, targetH);

                accumulatedX += targetW + SLOT_SPACING;
            }

            for (int i = 0; i < slotsList.Count; i++)
            {
                var slot = slotsList[i];
                BaseGun gun = GetGunForSlot(slot);
                if (gun == null) continue;

                if (slot.Progress < 1f)
                {
                    slot.Progress = Mathf.MoveTowards(slot.Progress, 1f, Time.deltaTime * TRANSITION_SPEED);
                    float t = slot.Progress * slot.Progress * (3f - 2f * slot.Progress);

                    slot.Rect.sizeDelta = Vector2.LerpUnclamped(slot.StartSize, targetSizes[slot], t);
                    slot.Rect.anchoredPosition = Vector2.LerpUnclamped(slot.StartPos, targetPositions[slot], t);
                }
                else
                {
                    slot.Rect.sizeDelta = targetSizes[slot];
                    slot.Rect.anchoredPosition = targetPositions[slot];
                }

                if (gun is IGunAmmo ammo)
                {
                    int ammoCount = ammo.RemainingAmmo;
                    if (slot.LastAmmo != ammoCount)
                    {
                        slot.LastAmmo = ammoCount;
                        slot.AmmoSlider.gameObject.SetActive(true);

                        int max = ammo.MaxAmmo;
                        float scaled = 1f + ((float)(ammoCount - 1) / (max - 1) * 13f);

                        slot.AmmoSlider.value = ammoCount <= 0 ? 0f :
                                                ammoCount >= max ? 15f :
                                                Mathf.Clamp(Mathf.Round(scaled), 1f, 14f);
                    }
                }
            }
        }

        private BaseGun GetGunForSlot(WeaponSlot slot)
        {
            foreach (var kvp in weaponToSlot)
            {
                if (kvp.Value == slot) return kvp.Key;
            }
            return null;
        }
    }
}