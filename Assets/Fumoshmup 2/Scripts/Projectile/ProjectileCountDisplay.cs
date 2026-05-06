using rinCore;
using TMPro;
using UnityEngine;

namespace FumoShmup2
{
    [RequireComponent(typeof(TMP_Text))]
    public class ProjectileCountDisplay : MonoBehaviour
    {
        TMP_Text bulletCountText;
        [SerializeField] bool useTick;
        private void Awake()
        {
            bulletCountText = GetComponent<TMP_Text>();
        }
        private void OnEnable()
        {
            if (useTick) TickManager.MainTickLightweight += RefreshUI;
        }
        private void OnDisable()
        {
            if (useTick) TickManager.MainTickLightweight -= RefreshUI;
        }
        private void Update()
        {
            if (!useTick) RefreshUI();
        }
        private static string PadToLength(string value, int targetLength, char fillChar = ' ')
        {
            if (value == null)
                value = string.Empty;

            if (value.Length > targetLength)
                return value.Substring(0, targetLength);

            int missing = targetLength - value.Length;

            if (missing > 0)
                return value + new string(fillChar, missing);

            return value;
        }
        public void RefreshUI()
        {
            if (bulletCountText == null)
                return;
            float simSlow = TimeSlowHandler.SimulatedSlowdown;
            bulletCountText.text = "P: " + PadToLength(ProjectileRunner.BulletCount.ToString(), 6, ' ') + (simSlow != 1f ? $" [{((1f / simSlow) * 100f - 100f).ToString("F0")}%]" : "");
        }
    }
}
