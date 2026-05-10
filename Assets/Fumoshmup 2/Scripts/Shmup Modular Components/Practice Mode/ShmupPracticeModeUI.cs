using UnityEngine;
using rinCore;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace FumoShmup2
{
    #region Warp Dropdown
    public partial class ShmupPracticeModeUI
    {
        static Dictionary<string, int> skipValueLookup = new();

        public static void BindSkipDropdown(List<ShmupStage.SkipValues> skips)
        {
            if (instance == null || instance.warpDropdown == null)
                return;

            skipValueLookup.Clear();
            foreach (var item in skips)
            {
                skipValueLookup[item.skipName] = item.skipValue;
            }

            instance.warpDropdown.ClearOptions();
            var options = new List<string>();
            foreach (var kvp in skips)
            {
                options.Add(kvp.skipName);
            }
            instance.warpDropdown.AddOptions(options);

            if (options.Count > 0)
            {
                int selectedIndex = 0;
                foreach (var kvp in skips)
                {
                    if (kvp.skipValue == ShmupPracticeMode.StageSkipValue)
                    {
                        selectedIndex = options.IndexOf(kvp.skipName);
                        break;
                    }
                }
                instance.warpDropdown.value = selectedIndex;
                instance.OnSelect(selectedIndex);
            }
        }
        private void OnSelect(int entry)
        {
            if (warpDropdown == null || entry < 0 || entry >= warpDropdown.options.Count || wasCreatedThisFrame)
            {
                ShmupPracticeMode.StageSkipValue = 0;
                return;
            }

            string selectedKey = warpDropdown.options[entry].text;
            if (skipValueLookup.TryGetValue(selectedKey, out int val) && ShmupPracticeMode.IsOn)
            {
                ShmupPracticeMode.StageSkipValue = val;
            }
            else
            {
                ShmupPracticeMode.StageSkipValue = 0;
            }
        }
    }
    #endregion
    public partial class ShmupPracticeModeUI : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown warpDropdown;
        [SerializeField] Slider bossWarp;
        [SerializeField] TMP_Text bossWarpText;
        [SerializeField] Toggle skipDialogue;
        [SerializeField] GameObject practiceModeNest;
        static bool skipDialogueValue = false;
        static ShmupPracticeModeUI instance;
        static ShmupStage currentStage;
        private static ShmupStage pendingStage;
        bool wasCreatedThisFrame = false;
        private void Awake()
        {
            instance = this;
            wasCreatedThisFrame = true;
            if (pendingStage != null)
            {
                LoadStageToPracticeMode(pendingStage);
                pendingStage = null;
            }
        }
        private void Start()
        {
            LoadOptions();
            if (ShmupPracticeMode.IsOn && currentStage != null)
            {
                RefreshUIForStage(currentStage);
            }
        }
        private void Update()
        {
            if (!ShmupPracticeMode.IsOn)
                return;
            if (ShmupInput.ReloadPracticeJustPressed)
            {
                OnQuickReset();
            }
        }
        private void LateUpdate()
        {
            wasCreatedThisFrame = false;
        }
        public static void LoadStageToPracticeMode(ShmupStage stage)
        {
            if (stage == null) return;

            bool sameStage = stage == currentStage;
            currentStage = stage;

            if (instance != null)
            {
                instance.RefreshUIForStage(stage);

                if (!sameStage)
                {
                    instance.warpDropdown.value = 0;
                    instance.OnSelect(0);
                }
            }
            else
            {
                pendingStage = stage;
            }
        }
        private void RefreshUIForStage(ShmupStage stage)
        {
            if (stage == null) return;
            practiceModeNest.SetActive(ShmupPracticeMode.IsOn);
            if (stage.SkipEntries != null)
            {
                int preservedSkipValue = ShmupPracticeMode.StageSkipValue;
                BindSkipDropdown(stage.SkipEntries);
                int selectedIndex = 0;
                for (int i = 0; i < stage.SkipEntries.Count; i++)
                {
                    if (stage.SkipEntries[i].skipValue == preservedSkipValue)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
                warpDropdown.value = selectedIndex;
                OnSelect(selectedIndex);

                warpDropdown.onValueChanged.RemoveAllListeners();
                warpDropdown.onValueChanged.AddListener(OnSelect);
            }
            skipDialogue.onValueChanged.RemoveAllListeners();
            skipDialogue.onValueChanged.AddListener(OnToggleSkipDialogue);
            OnToggleSkipDialogue(skipDialogueValue);

            bossWarp.onValueChanged.RemoveAllListeners();
            bossWarp.onValueChanged.AddListener(SetBossSkip);

            float preservedBossValue = ShmupPracticeMode.BossSkip;
            SetBossSkip(preservedBossValue);
            practiceModeNest.SetActive(ShmupPracticeMode.IsOn);
        }
        private void LoadOptions()
        {
            instance.practiceModeNest.SetActive(ShmupPracticeMode.IsOn);
            if (currentStage != null) BindSkipDropdown(currentStage.SkipEntries);

            warpDropdown.onValueChanged.RemoveAllListeners();
            warpDropdown.onValueChanged.AddListener(OnSelect);
            if (currentStage != null && currentStage.SkipEntries != null)
            {
                BindSkipDropdown(currentStage.SkipEntries);
            }

            skipDialogue.onValueChanged.RemoveAllListeners();
            skipDialogue.onValueChanged.AddListener(OnToggleSkipDialogue);
            OnToggleSkipDialogue(skipDialogueValue);

            bossWarp.onValueChanged.RemoveAllListeners();
            bossWarp.onValueChanged.AddListener(SetBossSkip);
            SetBossSkip(ShmupPracticeMode.BossSkip);
        }
        private void SetBossSkip(float value)
        {
            bossWarp.SetValues(value, 9, 0);
            ShmupPracticeMode.SetBossSkip(value.Floor().ToInt());
            bossWarpText.text = "Boss Warp : " + value.ToInt().ToString();
        }
        private void OnToggleSkipDialogue(bool value)
        {
            skipDialogueValue = value;
            skipDialogue.isOn = value;
        }

        private void OnQuickReset()
        {
            currentStage.RunStage(ShmupPracticeMode.StageSkipValue - 1);
        }
        public static bool SkipDialogue()
        {
            if (!ShmupPracticeMode.IsOn)
                return false;
            return skipDialogueValue;
        }
    }

}
