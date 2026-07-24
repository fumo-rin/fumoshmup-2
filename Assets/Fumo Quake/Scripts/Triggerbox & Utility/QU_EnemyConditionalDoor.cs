using rinCore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoQuake
{
    public class QU_EnemyConditionalDoor : QT_Base
    {
        [SerializeField]
        List<GameObject> Door = new();
        [SerializeField] List<QuakeEnemy> RequiredKilledEnemies = new();
        HashSet<QuakeEnemy> LiveList = new();
        protected override void WhenAwake()
        {
            LiveList = RequiredKilledEnemies.ToHashSet();
        }

        protected override bool WhenTriggerEnter(Collider other, IFumoUnit unit)
        {
            QuakeTextInfoUI.AddText("You most Destroy the enamies to pass!");
            return true;
        }
        private void OnEnable()
        {
            QuakeEnemy.WhenEnemyKilled += CheckKilledEnemy;
        }
        private void OnDestroy()
        {
            QuakeEnemy.WhenEnemyKilled -= CheckKilledEnemy;
        }
        private void CheckKilledEnemy(QuakeEnemy e)
        {
            LiveList.Remove(e);
            if (LiveList.Count <= 0)
            {
                QuakeEnemy.WhenEnemyKilled -= CheckKilledEnemy;
                foreach (var item in Door)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    Destroy(item.gameObject);
                    GeneralManager.FunnyExplosion(new()
                    {
                        is3d = true,
                        playSound = true,
                        position = item.transform.position,
                        scale = 3f
                    });
                }
            }
        }
    }
}
