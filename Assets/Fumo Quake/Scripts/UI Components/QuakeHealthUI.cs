using rinCore;
using UnityEngine;
using UnityEngine.UI;

namespace FumoQuake
{
    public class QuakeHealthUI : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        private const float MAX_HEALTH = 100f;
        public float Health100 => QuakeController.StoredHealth ?? 100f;
        public int Health20Step
        {
            get
            {
                float currentHealth = Health100;
                if (currentHealth <= 0f) return 0;
                if (currentHealth >= MAX_HEALTH) return 20;
                float percentage = currentHealth / MAX_HEALTH;
                int innerStep = Mathf.CeilToInt(percentage * 18f);
                return innerStep;
            }
        }
        private void LateUpdate()
        {
            healthSlider.SetValuesInt(Health20Step, 20, 0, false);
        }
    }
}