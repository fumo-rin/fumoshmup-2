using rinCore;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FumoShmup2
{
    public class BossbarUIAnimations : MonoBehaviour
    {
        public struct trackTarget
        {
            public ShmupUnit target;
            public int weight;
        }
        static BossbarUIAnimations instance;
        static List<trackTarget> targets = new();
        [SerializeField] Slider BossHealthbar;
        [SerializeField] TMP_Text BossNameText;
        [SerializeField] RectTransform notchObject;
        public static void FadeIn()
        {

        }
        public static void FadeOut()
        {

        }
        public static void AssignUnit(trackTarget target)
        {
            if (targets == null)
                targets = new List<trackTarget>();
        }
        public static void UnassignUnit(ShmupUnit target)
        {
            if (targets == null)
                targets = new List<trackTarget>();
            targets.RemoveAll(x => x.target == null);
            targets.RemoveAll(t => t.target == target);
        }
        private void DrawBar(ShmupUnit unit)
        {

        }
        private void Update()
        {
            if (targets == null)
                targets = new List<trackTarget>();
            targets.RemoveAll(x => x.target == null);
            if (targets.Count <= 0)
            {
                return;
            }
            ShmupUnit trackTarget = null;
            foreach (var target in targets.OrderBy(x => x.weight))
            {
                if (target.target == null)
                    continue;
                trackTarget = target.target;
                break;
            }
        }
    }
}
