using rinCore;
using UnityEngine;
using UnityEngine.UI;

namespace FumoShmup2
{
    #region Mode Toggle
    public partial class ShmupPracticeMode
    {
        public static bool IsOn => practiceModeToggle;

        private static bool practiceModeToggle;
        private static bool GetPracticeMode()
        {
            if (practiceModeToggle)
            {
                return true;
            }
            return false;
        }
        static ShmupPracticeMode()
        {
            GameSession.WhenInvalidationCheck += () => !IsOn;
        }
        public static void SetMode(bool state)
        {
            practiceModeToggle = state;
            PersistentJSON.TrySave(state, menuPracticeModeKey);

        }
        public static void Toggle() => SetMode(!practiceModeToggle);
    }

    #endregion
    public partial class ShmupPracticeMode : MonoBehaviour
    {
        public static int BossSkip { get; private set; } = 0;
        private static int skipValue;
        static string menuPracticeModeKey = "Shmup_Practice_Mode";
        public static int StageSkipValue
        {
            get => IsOn ? skipValue : 0;
            set
            {
                Debug.Log($"SkipValue changed from {skipValue} to {value}");
                skipValue = value;
            }
        }
        public static void SetBossSkip(int value) => BossSkip = value;
        [SerializeField] Toggle modeToggle;
        private void Awake()
        {
            modeToggle.isOn = false;
            modeToggle.onValueChanged.AddListener(WhenToggleButtonPress);
            if (PersistentJSON.TryLoad(out bool loadedPracticeMode, menuPracticeModeKey))
            {
                modeToggle.isOn = loadedPracticeMode;
                SetMode(loadedPracticeMode);
            }
        }
        private static bool ValidateScore()
        {
            return IsOn ? false : true;
        }
        private void OnDestroy()
        {
            modeToggle.onValueChanged.RemoveListener(WhenToggleButtonPress);
        }
        private void WhenToggleButtonPress(bool state)
        {
            SetMode(state);
        }
    }

}
