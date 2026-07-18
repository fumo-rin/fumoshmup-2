using rinCore;
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
            session.levelSequence = new()
            {
                pair
            };
            levelName.text = pair.name;
        }
    }
}
