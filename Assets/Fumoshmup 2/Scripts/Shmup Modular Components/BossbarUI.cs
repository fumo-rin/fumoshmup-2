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
        [SerializeField] TMP_Text BossNameText;
        [SerializeField] RectTransform notchObject;
        [SerializeField] RectTransform containerScaler;
        float anchoredSizeY;
        float currentSize;
        void FadeIn()
        {
            currentSize = currentSize.LerpEaseInOut(anchoredSizeY, Time.deltaTime * 18f);
            containerScaler.SetHeight(currentSize);
        }
        void FadeOut()
        {
            currentSize = currentSize.LerpEaseInOut(0f, Time.deltaTime * 20f);
            containerScaler.SetHeight(currentSize);
        }
        private void Awake()
        {
            anchoredSizeY = containerScaler.sizeDelta.y;
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
                return false;
            }
            string bossName = unit.transform.GetCleanName().PrettyName(new StringExtensions.PrettyNameSettings
            {
                PreserveBrackets = true,
                PostNaturalCapitals = true,
                PreserveNumbers = true,
                SpaceByCapitals = true
            });
            BossNameText.text = bossName;
            if (unit is EnemyUnit e)
            {
                BossHealthbar.SetValues(e.HealthPercent, 100f, 0f);
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
