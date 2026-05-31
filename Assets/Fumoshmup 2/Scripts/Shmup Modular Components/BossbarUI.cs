using rinCore;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FumoShmup2
{
    public class BossbarUI : MonoBehaviour
    {
        public struct trackTarget
        {
            public ShmupUnit target;
            public int weight;
        }
        static List<trackTarget> targets = new();
        [SerializeField] Slider BossHealthbar;
        [SerializeField] TMP_Text BossNameText, BossHealthText;
        [SerializeField] RectTransform containerScaler;
        [SerializeField] RectTransform notchItem;
        [SerializeField] CanvasGroup opacityNest;
        List<RectTransform> notches = new();

        float anchoredSizeY;
        float currentSize;

        void FadeIn()
        {
            currentSize = currentSize.LerpEaseInOut(anchoredSizeY, Time.deltaTime * 18f);
            opacityNest.alpha = currentSize.MapTo01(0f, anchoredSizeY * 0.1f, true);
            containerScaler.SetHeight(currentSize);
        }
        void FadeOut()
        {
            currentSize = currentSize.LerpEaseInOut(0f, Time.deltaTime * 20f);
            float fade = Mathf.InverseLerp(0f, anchoredSizeY * 12f, currentSize);
            opacityNest.alpha = fade;
            containerScaler.SetHeight(currentSize);
        }
        private void Awake()
        {
            anchoredSizeY = containerScaler.sizeDelta.y;
            notchItem.gameObject.SetActive(false);
        }
        public static void AssignUnit(trackTarget target)
        {
            if (targets == null)
                targets = new List<trackTarget>();
            foreach (var item in targets)
            {
                if (item.target == target.target)
                {
                    return;
                }
            }
            targets.Add(target);
        }
        public static void UnassignUnit(ShmupUnit target)
        {
            if (targets == null)
                targets = new List<trackTarget>();
            targets.RemoveAll(x => x.target == null || x.target.gameObject == null || !x.target.gameObject.activeInHierarchy);
            targets.RemoveAll(t => t.target == target);
        }
        private bool DrawBar(ShmupUnit unit)
        {
            if (unit == null)
            {
                BossNameText.text = "";
                BossHealthbar.SetValues(0f, 1f, 0f);
                BossHealthText.text = "";
                return false;
            }
            string bossName = unit.transform.GetCleanName().RemoveAfter("#").PrettyName(new StringExtensions.PrettyNameSettings
            {
                PreserveBrackets = true,
                PostNaturalCapitals = true,
                PreserveNumbers = true,
                SpaceByCapitals = true
            });
            BossNameText.text = bossName;
            if (unit is EnemyUnit e)
            {
                BossHealthbar.SetValues(e.HealthPercent100, 100f, 0f);
                SetupNotches(e);
                DamageText(e);
                void DamageText(EnemyUnit e)
                {
                    EnemyUnit.RecentDamage damageTaken = e.RecentDamageTaken;
                    string text = e.HealthString;
                    if (damageTaken.WindowTotal >= 1f)
                    {
                        text += $" -{damageTaken.EMA_PerSecond.Clamp(1f, 9999f).ToString("F0")}/s".Color(ColorHelper.PastelOrange);
                    }
                    BossHealthText.text = text;
                }
                void SetupNotches(EnemyUnit e)
                {
                    foreach (var item in notches)
                    {
                        GameObject g = item.gameObject;
                        if (g != null)
                        {
                            g.SetActive(false);
                        }
                    }
                    int index = 0;
                    foreach (var item in e.RemainingPhaseNotches01)
                    {
                        RectTransform iteration = null;
                        if (notches.Count > index)
                        {
                            iteration = notches[index];
                        }
                        if (iteration == null)
                        {
                            RectTransform pushed = Instantiate(notchItem, notchItem.parent);
                            iteration = pushed;
                            notches.Add(pushed);
                        }
                        if (iteration != null)
                        {
                            BossHealthbar.SetXPositionOfRect(notches[index], containerScaler, item);
                            notches[index].gameObject.SetActive(true);
                            index++;
                        }
                    }
                }
            }
            return true;
        }
        private void Update()
        {
            if (targets == null)
                targets = new List<trackTarget>();
            targets.RemoveAll(x => x.target == null);
            ShmupUnit trackTarget = null;
            if (targets.Count >= 0)
            {
                foreach (var target in targets.OrderBy(x => x.weight))
                {
                    if (target.target == null)
                        continue;
                    trackTarget = target.target;
                    break;
                }
            }
            if (DrawBar(trackTarget))
            {
                FadeIn();
            }
            else
            {
                FadeOut();
            }
        }
    }
}
