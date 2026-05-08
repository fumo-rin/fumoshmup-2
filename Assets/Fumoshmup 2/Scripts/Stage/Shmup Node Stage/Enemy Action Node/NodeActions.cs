using UnityEngine;

namespace FumoShmup2
{
    public abstract partial class NodeActions
    {
        [System.Serializable]
        public class ChasePlayerDownScreen : BaseAction
        {
            public float duration = 6f;
            public float speed = 8f;
            public float Speed
            {
                get
                {
                    return speed;
                }
            }

            public override void StartAction(EnemyUnit owner)
            {
                if (!ShmupPlayer.PlayerAs(out ShmupPlayer p))
                    return;
                owner.SetAction("Chase Player Down Screen", new ShmupEnemyActions.ChasePlayerDownScreen(owner, p, duration, duration * 2f, Speed));
            }
        }
        public abstract class BaseAction
        {
            public abstract void StartAction(EnemyUnit owner);
            public BaseAction()
            {

            }
        }
    }
}