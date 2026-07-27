using rinCore;
using System.Linq;
using UnityEngine;

namespace FumoQuake
{
    public class QuakeGameStarter : rinCore.FumoStartGameButton, IHierarchyComponentColor
    {
        [SerializeField] public QuakeSession session;
        public Color LabelColor => ColorHelper.PastelBlue.Opacity(50);

        protected override string LeaderboardKey => session.LeaderboardKey;
        protected override void StartGamePayload()
        {
            if (!GeneralManager.IsEditor)
            {
                session.LevelSequence = session.LevelSequence.Where(x => x.IncludeInBuild).ToList();
            }
            GameSession.StartSession(session);
        }
    }
}
