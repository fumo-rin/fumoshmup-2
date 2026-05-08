using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace FumoShmup2
{
    public class ShmupStageIndex : MonoBehaviour
    {
        [System.Serializable]
        private struct StageIndexEntry
        {
            public ShmupStage stage;
            public int gameModeIndex;
        }
        [SerializeField]
        List<StageIndexEntry> gameModeStages = new();
        public static Dictionary<int, Queue<ShmupStage>> CachedStages { get; private set; }
        private void Awake()
        {
            if (CachedStages == null) CachedStages = new();
            CachedStages.Clear();
            var list = gameModeStages.ToList();
            foreach (var item in list)
            {
                if (!CachedStages.TryGetValue(item.gameModeIndex, out Queue<ShmupStage> stageQueue))
                {
                    stageQueue = new Queue<ShmupStage>();
                    stageQueue.Enqueue(item.stage);
                    CachedStages[item.gameModeIndex] = stageQueue;
                    continue;
                }
                stageQueue.Enqueue(item.stage);
            }
        }
        public static Queue<ShmupStage> GetStagesForGamemode(int gamemode)
        {
            if (CachedStages == null)
            {
                Debug.LogError("CachedStages is null. Did ResetStageCache run? Is ShmupStageIndex Awake called?");
                return null;
            }
            if (!CachedStages.TryGetValue(gamemode, out Queue<ShmupStage> queue))
            {
                Debug.LogWarning("No Queue for Gamemode: " + gamemode.ToString());
                return null;
            }
            return queue;
        }
    }
}