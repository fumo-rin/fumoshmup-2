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
        public int Health20Step => Health100.MapTo01(0f, MAX_HEALTH).MapFrom01(0, 20).ToInt();
        private void LateUpdate()
        {
            healthSlider.SetValuesInt(Health20Step, 20, 0, false);
        }
    }
}