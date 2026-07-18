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
            public RectTransform Rect;
            public Slider AmmoSlider;
            public Vector2 StartSize;
            public Vector2 StartPos;
            public float Progress;
            public int LastAmmo = -1;
        }

        private const float UNSELECTED_WIDTH = 60f;
        private const float UNSELECTED_HEIGHT = 60f;
        private const float SELECTED_WIDTH = 120f;
        private const float SELECTED_HEIGHT = 120f;
        private const float SLOT_SPACING = 0f;
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

            foreach (var item in PlayerWeaponsController.LoadoutSnapshot)
            {
                BaseGun gun = item.Value;
                if (gun == null) continue;

                GameObject obj = Instantiate(itemTemplate, spawnedItemsNest);

                var slot = new WeaponSlot
                {
                    Rect = obj.GetComponent<RectTransform>(),
                    AmmoSlider = obj.GetComponentInChildren<Slider>(),
                    StartSize = new Vector2(UNSELECTED_WIDTH, UNSELECTED_HEIGHT),
                    StartPos = Vector2.zero,
                    Progress = 1f
                };

                if (obj.TryGetComponent(out Image img))
                {
                    img.sprite = gun.optionalIconUI;
                }

                slot.Rect.sizeDelta = slot.StartSize;

                slotsList.Add(slot);
                weaponToSlot.Add(gun, slot);
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
            float currentX = 0f;

            for (int i = 0; i < slotsList.Count; i++)
            {
                var slot = slotsList[i];
                BaseGun gun = GetGunForSlot(slot);
                bool isActive = (gun == activeGun);

                float targetW = isActive ? SELECTED_WIDTH : UNSELECTED_WIDTH;
                float targetH = isActive ? SELECTED_HEIGHT : UNSELECTED_HEIGHT;

                float pivotOffsetX = slot.Rect.pivot.x * targetW;
                Vector2 targetPos = new Vector2(currentX + pivotOffsetX, slot.Rect.anchoredPosition.y);
                Vector2 targetSize = new Vector2(targetW, targetH);

                if (slot.Progress < 1f)
                {
                    slot.Progress = Mathf.MoveTowards(slot.Progress, 1f, Time.deltaTime * TRANSITION_SPEED);
                    float t = slot.Progress * slot.Progress * (3f - 2f * slot.Progress);

                    slot.Rect.sizeDelta = Vector2.LerpUnclamped(slot.StartSize, targetSize, t);
                    slot.Rect.anchoredPosition = Vector2.LerpUnclamped(slot.StartPos, targetPos, t);
                }
                else
                {
                    slot.Rect.sizeDelta = targetSize;
                    slot.Rect.anchoredPosition = targetPos;
                }
                currentX += targetW + SLOT_SPACING;

                if (gun is IGunAmmo ammo)
                {
                    int ammoCount = ammo.RemainingAmmo;
                    if (slot.LastAmmo != ammoCount)
                    {
                        slot.LastAmmo = ammoCount;
                        slot.AmmoSlider.gameObject.SetActive(true);

                        int max = ammo.MaxAmmo;
                        float scaled = 1f + ((float)(ammoCount - 1) / (max - 1) * 13f);
                        slot.AmmoSlider.value = ammoCount <= 0 ? 0f : ammoCount >= max ? 15f : Mathf.Clamp(Mathf.Round(scaled), 1f, 14f);
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