using rinCore;
using UnityEngine;

namespace FumoQuake
{
    public class QuakeGameStarter : rinCore.FumoStartGameButton
    {
        [SerializeField] QuakeSession session;
        protected override string LeaderboardKey => throw new System.NotImplementedException();
        protected override void StartGamePayload()
        {
            GameSession.StartSession(session);
        }
    }
}
