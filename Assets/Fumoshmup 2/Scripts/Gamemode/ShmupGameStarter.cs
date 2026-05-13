using rinCore;
using UnityEngine;

namespace FumoShmup2
{
    public class ShmupGameStarter : rinCore.FumoStartGameButton, IHierarchyComponentColor
    {
        [SerializeField] ShmupSession session;
        [field: SerializeField] public Color LabelColor { get; set; } = ColorHelper.PastelCyan.Opacity(40);

        protected override string LeaderboardKey => session.SessionName;
        protected override void StartGamePayload()
        {
            ShmupSession.StartSession(session);
        }
    }
}
