using rinCore;
using UnityEngine;

namespace FumoShmup2
{
    public class MockupGamemodeLoader : MonoBehaviour
    {
        [SerializeField] ShmupGamemode dummyMode;
        private void Start()
        {
            if (dummyMode != null)
            {
                dummyMode.StartGamemode(0, new()
                {
                    RawExtrasScore = 0,
                    HighScore = 0,
                    RawScore = 0,
                    ScoreDivisor = 100d,
                    ScoreStorageKey = "BHJAM7"
                });
                if (ShmupGamemode.CurrentMode is ShmupGamemode mode && ShmupGamemode.TryGetCurrentShot(out ShmupGamemode.Shottype shot))
                {
                    mode.RequestPlayer(shot);
                }
            };
        }
    }
}