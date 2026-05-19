using rinCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FumoShmup2
{
    public class StandardShmupResourcesUI : MonoBehaviour
    {
        [SerializeField] TMP_Text difficultyText;
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
        private void Start()
        {
            if (!ShmupSession.CurrentAs(out ShmupSession sess) && difficultyText != null)
                return;
            difficultyText.text = sess.SessionDifficulty.Color(sess.DifficultyColor.Opacity(255));
        }
    }
}
