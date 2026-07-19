using System.Collections.Generic;
using UnityEngine;

namespace FumoQuake
{
    public interface IQuakeDeathTrigger
    {
        public void Run(QuakeEnemy sender);
    }
    public class QDT_ActivateEnemyNest : MonoBehaviour, IQuakeDeathTrigger
    {
        [SerializeField] List<Transform> activationNests = new();
        [SerializeField, Range(0.25f, 5f)] float delay = 0.25f;
        public void Run(QuakeEnemy sender)
        {
            foreach (var item in activationNests)
            {
                if (item == null)
                {
                    continue;
                }
                QuakeController.ActivateEnemyNestWithPortalEffect(item, delay);
            }
        }
    }
}
