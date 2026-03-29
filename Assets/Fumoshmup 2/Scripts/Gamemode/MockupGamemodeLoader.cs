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
                dummyMode.StartGamemode(0);
                if (ShmupGamemode.CurrentMode is ShmupGamemode mode && ShmupGamemode.TryGetCurrentShot(out ShmupGamemode.Shottype shot))
                {
                    mode.RequestPlayer(shot);
                }
            };
        }
    }
}