using UnityEngine;
using rinCore;
using TMPro;
using UnityEngine.Rendering;
using FumoShmup2;
namespace FumoShmup2
{
    #region Popup
    public partial class PlayerScoring
    {
        [NYI]
        public static void StartScorePopup(Vector2 pos, double value)
        {

        }
        [NYI]
        public static void AddToScorePopup(double value)
        {

        }
    }
    #endregion
    public partial class PlayerScoring : MonoBehaviour
    {
        public delegate void ScoreAction(double score, double highScore);
        public static event ScoreAction WhenScoreUpdate;

        public delegate void GrazeAction(int grazeCount);
        public static event GrazeAction WhenGrazeUpdate;

        public delegate void PickupAction();
        public static event PickupAction WhenPickup;

        public static double Score => 0D;
        public static double HighScore => 0D;
        public static float GrazeScoreMultiplier => 1f + (GrazeCount.AsFloat().Multiply(0.00001f));

        public static bool IsPracticeMode => false;
        public static int GrazeCount;

        private static double lastScore = -1;
        private static double lastHighScore = -1;
        private static int lastGraze = -1;
        [SerializeField] DamageNumberSO scoreNumber;
        static DamageNumberSO cachedScoreNumber;
        [Initialize(-777)]
        public static void ResetScoringState()
        {
            GrazeCount = 0;
            lastGraze = -1;
            lastScore = -1;
            lastHighScore = -1;
        }
        private void Awake()
        {
            cachedScoreNumber = scoreNumber;
        }
        private void Start()
        {
            ShmupSession.WhenContinue += WhenContinue;
        }
        private void OnDestroy()
        {
            GameSession.TryStoreSessionHighscore();
            ShmupSession.WhenContinue -= WhenContinue;
        }
        private static void WhenContinue()
        {
            ResetScoringState();
            RefreshScore();
            RefreshGraze();
        }
        public static void WhenEndGame()
        {

        }
        [NYI]
        public static void WhenStartGame()
        {
            ResetScoringState();
            RefreshScore();
            RefreshGraze();
        }
        [NYI("Scoring References for session")]
        public static double AddScoreWithoutMultiplier(double baseValue, string key, bool contributePopup = true)
        {
            bool noHighscore = IsPracticeMode;
            double score = baseValue;

            GameSession.TryAddScoreRaw(score, new(key));
            /*double score = GeneralManager.AddScore(value, noHighscore);
            if (!string.IsNullOrEmpty(key))
            {
                GeneralManager.AddScoreAnalysisKey(key, value);
            }*/
            if (contributePopup)
            {
                AddToScorePopup(((float)score));
            }
            RefreshScore();
            return score;
        }
        [QFSW.QC.Command("-addscore")]
        private static void CommandAddScore(double v)
        {
            AddScoreWithoutMultiplier(v, "Command", true);
        }
        public static double AddScore(double value, string key, bool contributePopup = true)
        {
            return AddScoreWithoutMultiplier(value * GrazeScoreMultiplier, key, contributePopup);
        }
        public static int AddGraze(int value)
        {
            GrazeCount += value;
            AddScoreWithoutMultiplier(1000d * value, "Grazing", false);
            return GrazeCount;
        }
        public static void AddPickupScore(double baseScore)
        {
            double score = AddScore(baseScore, "Pickups");
            WhenPickup?.Invoke();
        }
        public static void RefreshGraze()
        {
            WhenGrazeUpdate?.Invoke(GrazeCount);
        }
        public static void RefreshScore()
        {
            WhenScoreUpdate?.Invoke(Score, HighScore);
        }
        private void LateUpdate()
        {
            if (lastScore != Score || lastHighScore != HighScore)
            {
                lastScore = Score;
                lastHighScore = HighScore;
                RefreshScore();
            }

            if (lastGraze != GrazeCount)
            {
                lastGraze = GrazeCount;
                RefreshGraze();
            }
        }
    }
}
