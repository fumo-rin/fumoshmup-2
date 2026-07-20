using rinCore;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FumoQuake
{
    public class QuakeLevelSelector : rinCore.FumoStartGameButton, IHierarchyComponentColor
    {
        [SerializeField] QuakeSession session;
        [SerializeField] TMP_Text levelName;
        public Color LabelColor => ColorHelper.PastelBlue.Opacity(50);
        protected override string LeaderboardKey => session.LeaderboardKey;
        protected override void StartGamePayload()
        {
            GameSession.StartSession(session);
        }
        public void SetLevel(rinCore.ScenePairSO pair)
        {
            session.LevelSequence = new()
            {
                pair
            };
            levelName.text = pair.name.PrettyName(new()
            {
                PostNaturalCapitals = false,
                PreserveNumbers = true,
                PreserveUnderscore = false,
                PreserveBrackets = false,
                RemoveSpaces = false,
                SpaceByCapitals = false
            }).ReplaceLineBreaks("#");
        }
    }
}
