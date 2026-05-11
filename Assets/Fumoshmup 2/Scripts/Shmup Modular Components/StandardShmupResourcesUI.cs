using rinCore;
using UnityEngine;
using UnityEngine.UI;

namespace FumoShmup2
{
    public class StandardShmupResourcesUI : MonoBehaviour
    {
        [SerializeField] Slider playerLivesSlider, playerBombsSlider;
        [SerializeField] int maxLives, maxBombs;
        private void LateUpdate()
        {
            if (!ShmupSession.CurrentAs(out ShmupSession sess))
                return;
            int lives = sess.GetInt(ShmupSession.keys.CurrentLives);
            int bombs = sess.GetInt(ShmupSession.keys.CurrentBombs);
            playerLivesSlider.SetValuesInt(lives, maxLives, 0);
            playerBombsSlider.SetValuesInt(bombs, maxBombs, 0);
        }
    }
}
