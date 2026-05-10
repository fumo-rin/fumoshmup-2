using rinCore;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FumoShmup2
{
    public class EnemyIndicator : MonoBehaviour
    {
        [SerializeField] Slider enemySlider;
        [SerializeField] Transform sliderAnchor;
        static EnemyIndicator instance;
        [rinCore.Initialize(0)]
        private void Awake()
        {
            instance = this;
            enemySlider.gameObject.SetActive(false);
        }
        public static void TrackUnit(FumoUnit tracked)
        {
            if (instance == null || tracked == null)
            {
                return;
            }
            if (instance.enemySlider == null)
            {
                Debug.LogWarning("missing enemy indicator slider");
                return;
            }
            IEnumerator CO_Track(FumoUnit tracked)
            {
                Slider s = Instantiate(instance.enemySlider, instance.sliderAnchor);
                s.gameObject.SetActive(true);
                s.maxValue = ShmupWorldspace.WorldSpace.max.x;
                s.minValue = ShmupWorldspace.WorldSpace.min.x;
                while (tracked != null)
                {
                    s.value = tracked.CurrentPosition.x;
                    yield return null;
                }
                Destroy(s.gameObject);
            }
            instance.StartCoroutine(CO_Track(tracked));
        }
    }
}
