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
        [Initialize(0)]
        private static void ResetCachedValues()
        {
            cachedInvincibility = null;
            cachedSkipDialogue = null;
        }
        static bool? cachedSkipDialogue;
        public static bool SkipDialogue
        {
            get
            {
                if (cachedSkipDialogue != null)
                {
                    return cachedSkipDialogue.Value;
                }
                const string KEY = "Practice Mode Skip Dialogue";
                bool Value = IsOn;
                if (!Value)
                {
                    cachedSkipDialogue = false;
                    return false;
                }
                PersistentJSON.TryLoad(out Value, KEY);
                cachedSkipDialogue = Value;
                return Value;
            }
            set
            {
                const string KEY = "Practice Mode Skip Dialogue";
                PersistentJSON.TrySave(value, KEY);
                cachedSkipDialogue = value;
            }
        }
        static bool? cachedInvincibility;
        public static bool Invincibility
        {
            get
            {
                if (cachedInvincibility != null)
                {
                    return cachedInvincibility.Value;
                }
                const string KEY = "Practice Mode Invincibility";
                bool Value = IsOn;
                if (!Value)
                {
                    cachedInvincibility = false;
                    return false;
                }
                PersistentJSON.TryLoad(out Value, KEY);
                cachedInvincibility = Value;
                return Value;
            }
            set
            {
                const string KEY = "Practice Mode Invincibility";
                PersistentJSON.TrySave(value, KEY);
                cachedInvincibility = value;
            }
        }

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
