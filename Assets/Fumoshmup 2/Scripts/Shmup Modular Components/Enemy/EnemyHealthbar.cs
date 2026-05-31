using rinCore;
using UnityEngine;
using UnityEngine.UI;

namespace FumoShmup2
{
    public class EnemyHealthbar : MonoBehaviour
    {
        EnemyUnit Owner;
        [SerializeField] Image healthImage, backdropImage;
        private void Awake()
        {
            bool found = this.TryGetComponentUpwards(out Owner);
            if (!found)
            {
                gameObject.SetActive(false);
            }
        }
        private void SetActive(bool state, params Image[] items)
        {
            foreach (var item in items)
            {
                item.enabled = state;
            }
        }
        private void LateUpdate()
        {
            float healthLerp = Owner.HealthPercent100.MapTo01(0f, 100f, true);
            SetActive(healthLerp <= 0.9f, healthImage, backdropImage);
            healthImage.rectTransform.anchorMax = new(healthLerp, 1f);
        }
    }
}
