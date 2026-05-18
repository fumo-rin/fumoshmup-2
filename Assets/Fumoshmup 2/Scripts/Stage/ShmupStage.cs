using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Search;

namespace FumoShmup2
{
    #region Shortcuts
    public partial class ShmupStage
    {
        public bool Spawn(EnemyUnit enemy, out EnemyUnit result, Vector2 position, MoveLerpAction.LerpSettings? lerpSettings)
        {
            return StageTools.Spawn(enemy, out result, position, lerpSettings);
        }
        public Vector2 Map(float x, float y, out Vector2 mapped)
        {
            return StageTools.Map(x, y, out mapped);
        }
        public bool SpawnBoss(EnemyUnit e, out EnemyUnit result, out WaitUntil wait)
        {
            return StageTools.SpawnBoss(e, out result, out wait);
        }
        public void SectionSweep(byte loot = 255)
        {
            StageTools.SectionSweep(loot);
        }
        public void StartDialogue(DialogueStackSO d, out WaitUntil w, Action whenEndDialogue = null)
        {
            StageTools.StartDialogue(d, out w, whenEndDialogue);
        }
        protected WaitUntil WaitForTimeOrEnemyCountLessThan(float duration, int enemyCount)
        {
            return StageTools.WaitForTimeOrEnemyCountLessThan(duration, enemyCount);
        }
        public WaitUntil WaitForKill(EnemyUnit unit)
        {
            return StageTools.WaitForKill(unit);
        }
    }
    #endregion
    public abstract partial class ShmupStage : ScriptableObject
    {
        [System.Serializable]
        public struct SkipValues
        {
            public string skipName;
            public int skipValue;
            public bool enabled;
        }
        [field: SerializeField] public List<SkipValues> SkipEntries = new();
        protected static EnemyUnit enemyIteration;
        protected static Vector2 vec2Iteration;
        [field: SerializeField] public ScenePairSO StageScene { get; private set; }
        public static bool RanStageThisFrame { get; private set; }
        public delegate void RequestSpawnPlayer(Vector2Shmup position);
        public static RequestSpawnPlayer WhenSpawnPlayerRequest;
        public void RunStage(int skip)
        {
            IEnumerator CO_FlickRanStage()
            {
                yield return null;
                RanStageThisFrame = false;
            }
            RanStageThisFrame = true;
            Debug.Log("Run Stage : " + skip);
            ShmupPracticeModeUI.LoadStageToPracticeMode(this);
            Dialogue.Stop();

            ProjectileRunner.TriggerSweep(0.5f, 0, false, out _);
            foreach (var item in EnemyUnit.AliveEnemies.ToArray())
                item.ForceKill();
            ProjectileRunner.TriggerSweep(0f, 0, false, out _);

            StopStage();
            GlobalCoroutineRunner.StartRoutine("Stage", StagePayload(skip));
            WhenSpawnPlayerRequest?.Invoke(new Vector2Shmup(0.5f, 0.2f));
            CO_FlickRanStage().RunRoutine();
        }
        public static void StopStage()
        {
            GlobalCoroutineRunner.StopAllOfKey("Stage", "Stage Extras");
        }
        protected abstract IEnumerator StagePayload(int skip);
    }
}