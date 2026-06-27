using QFSW.QC.Utilities;
using rinCore;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FumoShmup2
{
    public class LevelSelectSubmitter : MonoBehaviour
    {
        [System.Serializable]
        struct levelInfoTextComponents
        {
            public TMP_Text levelName, levelCredits, missionDescription, debugText;
            public void ClearText()
            {
                levelName.text = "";
                levelCredits.text = "";
                missionDescription.text = "";
                debugText.text = "";
            }
            public void LoadFromText(MarioLevelSelectItem level)
            {
                TextAsset a = level.StageInfo;
                if (a != null)
                {
                    Dictionary<string, string> tags = a.ParseTags();
                    if (tags.TryGetValue("levelName", out var name))
                        levelName.text = name;
                    if (tags.TryGetValue("levelCredits", out var credits))
                        levelCredits.text = credits;
                    if (tags.TryGetValue("missionDescription", out var mission))
                        missionDescription.text = mission;
                }
                debugText.text = "";
#if UNITY_EDITOR
                foreach (var item in level.AttachedStages)
                {
                    debugText.text += $"{item.name}##";
                    debugText.text = debugText.text.ReplaceLineBreaks("##");
                }
#endif
            }
        }
        [SerializeField] ShmupSession session;
        MarioLevelSelectItem currentSelection;
        [SerializeField] Button startButton;
        [SerializeField] levelInfoTextComponents levelDetails;
        void StartGame(MarioLevelSelectItem level)
        {
            session.ExternallyAssignStages(level.AttachedStages);
            ShmupSession.StartSession(session);
        }
        private void AssignSelection(MarioLevelSelectItem item)
        {
            currentSelection = item;
            levelDetails.LoadFromText(item);
        }
        private void StartCurrent()
        {
            if (currentSelection is MarioLevelSelectItem validSelection)
            {
                StartGame(validSelection);
            }
        }
        private void OnEnable()
        {
            startButton.onClick.AddListener(StartCurrent);
            MarioLevelSelectItem.WhenLevelSelected += AssignSelection;
        }
        private void OnDisable()
        {
            startButton.onClick.RemoveListener(StartCurrent);
            MarioLevelSelectItem.WhenLevelSelected -= AssignSelection;
        }
        private void Awake()
        {
            levelDetails.ClearText();
        }
        private void Start()
        {
            if (MarioLevelSelectItem.LoadStored(out MarioLevelSelectItem stored))
            {
                AssignSelection(stored);
            }
        }
    }
}