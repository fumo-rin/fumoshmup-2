using rinCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FumoShmup2
{
    public abstract class ShmupPlayerShooter : MonoBehaviour
    {
        protected static float TICK => UnitAttack.TICK;
        [SerializeField] protected int Dps_Unfocus = 600, Dps_Focus = 440;
        [SerializeField] protected ShmupUnit Owner;
        protected AttackBuilder a = new();
        protected HashSet<Projectile> shotcap = new();

        protected static float LockedAttackTime;
        protected bool LockedAttack => Time.time < LockedAttackTime;

        protected static Coroutine CurrentShotAction;

        [Initialize(100)]
        static void ResetCurrentAction()
        {
            CurrentShotAction = null;
        }

        [System.Serializable]
        public struct ShootingState
        {
            [SerializeField] InputActionReference shootingAction, focusAction, powerFireAction;
            public bool Shooting => !shootingAction.ReleasedLongerThan(0.08f);
            public bool Focus => !focusAction.ReleasedLongerThan(0.08f);
            public bool PowerFire => !powerFireAction.ReleasedLongerThan(0.08f);
            public bool PowerFireTap => powerFireAction.IsPressedRaw() && !powerFireAction.PressedLongerThan(0.02f);
        }
        public ShootingState ShootState = new();
        protected abstract void WhenUpdate();
        protected abstract void WhenEnable();
        private void Update()
        {
            shotcap.RemoveWhere(x => x == null || !x.IsActive || !x.isOnScreen);
            if (Time.time < LockedAttackTime)
                return;
            WhenUpdate();
        }
        private void OnEnable()
        {
            CurrentShotAction = null;
            WhenEnable();
        }
        protected bool TryStartShot(IEnumerator e, ref Coroutine channel, bool stopOther)
        {
            if (channel != null)
            {
                if (!stopOther)
                    return false;

                GlobalCoroutineRunner.StopAllOfKey("Player Shot");
            }
            channel = e.RunRoutine("Player Shot", false);
            return true;
        }
    }
}
