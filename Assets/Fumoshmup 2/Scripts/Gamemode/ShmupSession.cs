using rinCore;
using System.Collections.Generic;
using UnityEngine;
namespace FumoShmup2
{
    #region Game Actions
    public partial class ShmupSession
    {
        public delegate void GameAction();
        public static event GameAction WhenContinue;
        public static void TriggerContinue()
        {
            WhenContinue?.Invoke();
        }
    }
    #endregion
    #region Game Start
    public partial class ShmupSession
    {
        [field: SerializeField] public ShmupGamemode Gamemode { get; private set; }
        protected override void WhenStartSession()
        {
            stageIndex.LoadStagesToQueue();
            ShmupGamemode.SetCurrent(Gamemode);
            if (!stageIndex.TryGetNextStage(out ShmupStage next))
            {
                Debug.LogError("bwuz");
            }
            ShmupStage.WhenSpawnPlayerRequest = ShmupGamemode.SpawnCurrentPlayer; // this is without event tag so it can be = nulled
            next.StageScene.Load(() => next.RunStage(0));
        }
        protected override void WhenEndSession()
        {

        }
    }
    #endregion
    #region Stage
    public partial class ShmupSession
    {
        [SerializeField] ShmupStageIndex stageIndex = new();
        public void LoadNextStageOrMenu() => stageIndex.GoNextStageOrMenu();

        [System.Serializable]
        class ShmupStageIndex
        {
            [SerializeField] ScenePairSO mainMenuScene;
            [SerializeField] List<ShmupStage> gameModeStages = new();
            Queue<ShmupStage> StageQueue;
            public void LoadStagesToQueue()
            {
                StageQueue = new();
                foreach (var item in gameModeStages)
                {
                    StageQueue.Enqueue(item);
                }
            }
            public bool TryGetNextStage(out ShmupStage next)
            {
                next = null;
                if (StageQueue == null || StageQueue.Count <= 0)
                {
                    return false;
                }
                next = StageQueue.Dequeue();
                return true;
            }
            public void GoNextStageOrMenu()
            {
                void RunNext(ShmupStage s)
                {
                    ShmupPracticeMode.StageSkipValue = 0;
                    s.RunStage(ShmupPracticeMode.StageSkipValue - 1);
                }
                if (TryGetNextStage(out ShmupStage stage))
                {
                    if (stage.StageScene != null)
                    {
                        stage.StageScene.Load(() => RunNext(stage));
                    }
                    else
                    {
                        RunNext(stage);
                    }
                }
                else
                {
                    GameSession.EndSessionSettings end = new()
                    {
                        SubmitScore = true
                    };
                    SceneLoader.LoadScenePair(mainMenuScene, () => ShmupSession.EndSession(end));
                }
            }
        }
    }
    #endregion
    #region Shmup ECO Keys
    public partial class ShmupSession
    {
        public struct keys
        {
            public static string CurrentLives => "CurrentLives";
            public static string StartingLives => "StartingLives";
            public static string CurrentBombs => "CurrentBombs";
            public static string StartingBombs => "StartingBombs";
            public static string HitCounter => "Hit";
            public static string CashoutActivation060 => "Cashout";
        }
    }
    #endregion
    [System.Serializable]
    public partial class ShmupSession : rinCore.GameSession
    {
        public string SessionName => cachedSessionName;
        [SerializeField] private string cachedSessionName = "Game Name";
        public string SessionDifficulty = "Ultra";
        public Color32 DifficultyColor = ColorHelper.PastelCyan;
        public static bool SkipDialogue;
        public class shmupPlayerResources
        {
            Dictionary<string, int> intTable = new Dictionary<string, int>()
            {
                { keys.CurrentLives, 2},
                { keys.StartingLives, 2},
                { keys.CurrentBombs, 3 },
                { keys.StartingBombs, 3 },
            };
            Dictionary<string, float> floatTable = new();
            public shmupPlayerResources SetInt(string key, int newValue)
            {
                intTable[key] = newValue;
                return this;
            }
            public shmupPlayerResources SetFloat(string key, float newValue)
            {
                floatTable[key] = newValue;
                return this;
            }
            public float GetFloat(string key)
            {
                float value = 0;
                if (floatTable == null)
                {
                    Debug.LogError("Bwuz");
                    return 0f;
                }
                if (!floatTable.TryGetValue(key, out value))
                {
                    floatTable[key] = 0;
                    return 0f;
                }
                return value;
            }
            public int GetInt(string key)
            {
                int value = 0;
                if (intTable == null)
                {
                    Debug.LogError("Bwuz");
                    return 0;
                }
                if (!intTable.TryGetValue(key, out value))
                {
                    intTable[key] = 0;
                    return 0;
                }
                return value;
            }
        }
        public override bool GameLogicStalled
        {
            get
            {
                if (Time.timeScale <= 0f) return true;
                if (GeneralManager.IsPaused) return true;
                if (Dialogue.IsRunning) return true;
                if (base.GameLogicStalled)
                {
                    return true;
                }
                return false;
            }
        }
        [field: SerializeField] public ACWrapper SweepSound { get; private set; }
        public shmupPlayerResources playerResources = new();
        #region Int
        public int GetInt(string key) => playerResources.GetInt(key);
        public int SetInt(string key, int value, int min, int max) => playerResources.SetInt(key, value.Clamp(min, max)).GetInt(key);
        public int ChangeInt(string key, int delta, int min, int max) => playerResources.SetInt(
            key,
            (playerResources.GetInt(key) + delta).Clamp(min, max)
            ).GetInt(key);
        #endregion
        #region Float
        public float GetFloat(string key) => playerResources.GetFloat(key);
        public float SetFloat(string key, float value, float min, float max) => playerResources.SetFloat(key, value.Clamp(min, max)).GetFloat(key);
        public float ChangeFloat(string key, float delta, float min, float max)
        {
            float current = playerResources.GetFloat(key);
            current += delta;
            current = current.Clamp(min, max);
            playerResources.SetFloat(key, current);
            return current;
        }

        #endregion
    }
}
