using rinCore;
using System.Collections.Generic;
using UnityEngine;
namespace FumoShmup2
{
    #region Shmup Scoring
    public partial class ShmupSession
    {
        public virtual float PickupValue(float multiplier)
        {
            float hitValue = 0;
            if (ShmupSession.CurrentAs(out ShmupSession sess))
            {
                hitValue = 0.1f * sess.GetFloat(ShmupSession.keys.HitCount).Clamp(0f, 99999f);
            }
            return (hitValue + 1000f) * multiplier;
        }
    }
    #endregion
    #region Game Start
    public partial class ShmupSession
    {
        [field: SerializeField] public ShmupGamemode Gamemode { get; private set; }
        protected override void WhenStartSession()
        {
            Dialogue.TrySetPlayerCharacter(playerSpeaker);
            stageIndex.LoadStagesToQueue();
            ShmupGamemode.SetCurrent(Gamemode);
            if (!stageIndex.TryGetNextStage(out ShmupStage next))
            {
                Debug.LogError("bwuz");
            }
            ShmupStage.WhenSpawnPlayerRequest = ShmupGamemode.SpawnCurrentPlayer; // this is without event tag so it can be = nulled
            SceneLoader.LoadScenePair(next.StageScene, () => next.RunStage(0), 0.25f);
            PointItemRunner.WhenPointItemValue = PickupValue;
        }
        protected override void WhenEndSession()
        {
            PointItemRunner.WhenPointItemValue -= PickupValue;
        }
    }
    #endregion
    #region Stage
    public partial class ShmupSession
    {
        [SerializeField] ShmupStageIndex stageIndex = new();
        public void LoadNextStageOrMenu() => stageIndex.GoNextStageOrMenu();
        public void ExitToMenu() => stageIndex.GoMenu();

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
                        SceneLoader.LoadScenePair(stage.StageScene, () => RunNext(stage), 0.25f);
                    }
                    else
                    {
                        Debug.LogError($"Missing Stage on: {stage.name}");
                        RunNext(stage);
                    }
                }
                else
                {
                    GoMenu();
                }
            }
            public void GoMenu()
            {
                GameSession.EndSessionSettings end = new()
                {
                    SubmitScore = true
                };
                SceneLoader.LoadScenePair(mainMenuScene, () => ShmupSession.EndSession(end));
            }
        }
    }
    #endregion
    #region Continue
    public partial class ShmupSession
    {
        public delegate void GameAction();
        public static event GameAction WhenContinue;
        public void TryContinue()
        {
            SetInt(keys.CurrentLives, GetInt(keys.StartingLives), 0, 6);
            SetInt(keys.CurrentBombs, GetInt(keys.StartingBombs), 0, 6);
            SetFloat(keys.HitCount, 0f, 0f, 99999f);
            scoringData.Continue();
            WhenContinue?.Invoke();
        }
    }
    #endregion
    #region Shmup ECO Key
    public partial class ShmupSession // Keys
    {
        public struct keys
        {
            public static readonly string CurrentLives = "CurrentLives";
            public static readonly string StartingLives = "StartingLives";
            public static readonly string CurrentBombs = "CurrentBombs";
            public static readonly string StartingBombs = "StartingBombs";
            public static readonly string HitCount = "Hit";
        }
    }
    #endregion
    [System.Serializable]
    public partial class ShmupSession : rinCore.GameSession
    {
        public bool CanContinue = true;
        [SerializeField] DialogueCharacterSO playerSpeaker;
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
