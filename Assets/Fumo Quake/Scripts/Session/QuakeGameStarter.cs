using rinCore;
using UnityEngine;

namespace FumoQuake
{
    public class QuakeGameStarter : rinCore.FumoStartGameButton, IHierarchyComponentColor
    {
        [SerializeField] QuakeSession session;
        public Color LabelColor => ColorHelper.PastelBlue.Opacity(50);

        protected override string LeaderboardKey => session.LeaderboardKey;
        protected override void StartGamePayload()
        {
            GameSession.StartSession(session);
        }
    }
}
