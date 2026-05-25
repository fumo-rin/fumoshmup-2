using FumoShmup2;
using rinCore;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FumoShmup2
{
    #region Iframes
    public abstract partial class ShmupUnit
    {
        public abstract bool HasIframes { get; }
        public float CurrentIFramesDamageReductionPercent = 0f;
    }
    #endregion
    #region Attack Movement
    public partial class ShmupUnit
    {
        protected static Rect BuildBossRect(Vector2 bottomLeft01, Vector2 topRight01)
        {
            return Rect.MinMaxRect(bottomLeft01.x, bottomLeft01.y, topRight01.x, topRight01.y);
        }
        Coroutine runningAttackMovement;
        public bool IsMovingWithAttack => runningAttackMovement != null;
        public void StartMovement(IEnumerator Co, out WaitUntil wait, Action runAfter = null)
        {
            IEnumerator Wrap(IEnumerator Co)
            {
                if (this is EnemyUnit e)
                {
                    e.StopLeash();
                }
                yield return StartCoroutine(Co);
                runningAttackMovement = null;
                runAfter?.Invoke();
                if (this is EnemyUnit e2)
                {
                    e2.SetLeashPosition(CurrentPosition);
                }
            }
            runningAttackMovement = StartCoroutine(Wrap(Co));
            wait = new WaitUntil(() => !IsMovingWithAttack);
        }
        public class Testing
        {
            public static IEnumerator CO_TestDash(ShmupUnit sender)
            {
                Vector2 pos = sender.CurrentPosition + RNG.SeededRandomVector2 * RNG.FloatRange(3.5f, 5.5f);
                ShmupWorldspace.ClampToNormalizedSpace(pos, BuildBossRect(new(0.1f, 0.5f), new(0.9f, 0.8f)), out Vector2 clampedPos, ShmupWorldspace.WorldSpace);
                float durationLeft = 0.75f;
                Vector2 start = sender.CurrentPosition;
                while (durationLeft > 0f)
                {
                    float lerp = durationLeft.MapTo01(0.75f, 0f, true);
                    lerp = LerpCurves.EaseOutElastic(lerp);
                    sender.SetPosition(start.LerpUnclamped(clampedPos, lerp));
                    durationLeft -= Time.deltaTime;
                    yield return null;
                }
            }
        }
    }
    #endregion
    public abstract partial class ShmupUnit : FumoUnit
    {
        public readonly IShmupMover fallbackMover = new ShmupMovers.PlayerShmupMover(5f);
        public List<IShmupMover> shmupMovers = new();
        protected override bool CalculateAlive()
        {
            return gameObject != null && gameObject.activeInHierarchy;
        }
        protected override void WhenAwake()
        {
            if (this is not ShmupPlayer p)
            {
                MaintainAliveEnemy(this, new()
                {
                    ForceOverride = true,
                    OverrideAliveState = true
                });
            }
        }
        protected override void WhenDestroy()
        {
            if (this is not ShmupPlayer p)
            {
                MaintainAliveEnemy(this, new()
                {
                    ForceOverride = false,
                    OverrideAliveState = false
                });
            }
        }
        private void OnDisable()
        {
            ForceRemoveAliveEnemy(this);
        }
        private void OnEnable()
        {
            runningAttackMovement = null;
        }
        protected override void WhenStart()
        {

        }
        protected override void WhenUpdate()
        {

        }
    }
}