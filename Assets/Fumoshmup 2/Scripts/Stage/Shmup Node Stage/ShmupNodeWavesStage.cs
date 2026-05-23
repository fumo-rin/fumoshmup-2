using rinCore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoShmup2
{
    [CreateAssetMenu(menuName = "FumoShmup2/Stages/Fumo Node Waves Stage")]
    public class ShmupNodeWavesStage : ShmupNodeStage
    {
        public int wavesCount = 50;
        public int bossEveryWaves = 5;
        protected override IEnumerator StagePayload(int skip)
        {
            int currentWave = 1;
            yield return 0.15f.WaitForSeconds();

            for (int i = 0; i < wavesCount; i++)
            {
                int attempts = 100;
                while (attempts > 0)
                {
                    if (currentWave > 0 && currentWave % bossEveryWaves == 0)
                    {
                        currentWave++;
                        yield return StageTools.WaitForTimeOrEnemyCountLessThan(99f, 1);
                        yield return RandomBossSection();
                        attempts = 0;
                        continue;
                    }
                    int randomSkip = SkipEntries.OrderByRandom().First().skipValue;
                    if (IsBossSection(randomSkip))
                    {
                        attempts--;
                        continue;
                    }
                    currentWave++;
                    yield return CollectAndRunSkip(randomSkip);
                    yield return StageTools.WaitForTimeOrEnemyCountLessThan(7f - (currentWave.AsFloat(0.1f)).Clamp(0f, 4.5f), 1);
                    attempts = 0;
                    continue;
                }
            }
            yield return StageTools.WaitForTimeOrEnemyCountLessThan(999f, 1);
            StartDialogue(StageEndDialogue, out WaitUntil w, null);
            yield return w;
            yield return 2.5f.WaitForSeconds();
            if (ShmupSession.CurrentAs(out ShmupSession sess))
            {
                sess.LoadNextStageOrMenu();
            }
        }
        private bool IsBossSection(int skip)
        {
            var orderedNodes = nodes.Where(n => n != null && n.skipIndex == skip && n.IsEnabled)
                .ToList();

            foreach (var item in orderedNodes)
            {
                if (item is BossNode b)
                {
                    return true;
                }
            }
            return false;
        }
        private IEnumerator RandomBossSection()
        {
            var orderedNodes = nodes.Where(n => n != null && n.IsEnabled && n is BossNode b)
                .OrderByRandom()
                .ToList();

            foreach (var item in orderedNodes)
            {
                if (item is IStageNodeRunable runable)
                {
                    yield return runable.RunNode();
                    yield break;
                }
            }
        }
    }
}
