using rinCore;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FumoQuake
{
    public class QDT_ActivationAfterKills : MonoBehaviour, IQuakeDeathTrigger, IHierarchyComponentColor
    {
        public Color LabelColor => ColorHelper.PastelOrange.Opacity(50);
        [SerializeField] List<Transform> activationNests = new();
        [SerializeField] List<QuakeEnemy> killRequirement = new();
        HashSet<QuakeEnemy> liveList = new();
        private void Awake()
        {
            liveList = killRequirement.ToHashSet();
            foreach (var item in activationNests)
            {
                if (item == null)
                    continue;
                item.gameObject.SetActive(false);
            }
        }
        private void OnEnable()
        {
            QuakeEnemy.WhenEnemyKilled += Run;
        }
        private void OnDestroy()
        {
            QuakeEnemy.WhenEnemyKilled -= Run;
        }
        public void Run(QuakeEnemy sender)
        {
            liveList.Remove(sender);
            liveList.RemoveWhere(x => x == null || !x.IsAlive);
            if (liveList.Count > 0)
                return;
            foreach (var item in activationNests)
            {
                if (item == null)
                {
                    continue;
                }
                item.gameObject.SetActive(true);
            }
        }
    }
}
