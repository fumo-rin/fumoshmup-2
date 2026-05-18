using rinCore;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FumoShmup2
{
    public abstract class PlayerBomb : MonoBehaviour
    {
        static PlayerBomb active;
        static bool IsValidAndActive => active != null && active.gameObject != null && active.gameObject.activeInHierarchy;
        public static bool CanBomb
        {
            get
            {
                return active != null && active.canBomb;
            }
        }
        protected abstract bool canBomb { get; }
        private void Awake()
        {
            active = this;
            WhenAwake();
        }
        protected abstract void WhenAwake();
        public static bool TryTriggerBomb(ShmupUnit Owner)
        {
            if (IsValidAndActive && CanBomb && active is PlayerBomb b)
            {
                GlobalCoroutineRunner.StartRoutine("Player Bomb", b.BombPayload(Owner), false);
                return true;
            }
            return false;
        }
        protected abstract IEnumerator BombPayload(ShmupUnit Owner);
    }
}
