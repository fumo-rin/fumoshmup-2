using FumoShmup2;
using rinCore;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

#region Hit
public partial class ShmupPlayer : IHit
{
    Coroutine currentHit;
    [System.Serializable]
    class HitRoutineData
    {
        public Volume HitVolume;
        public ACWrapper HitSound;
        public ACWrapper DeathSound;
    }
    [SerializeField] HitRoutineData hitData = new();
    private float iframesEndTime = 0f;
    public delegate void IframesDurationActivation(float duration);
    public static event IframesDurationActivation WhenIframesActivatedGetDuration;
    public void SetCurrentIFrames(float duration)
    {
        CurrentIFramesDamageReductionPercent = 100f;
        iframesEndTime = Time.time + duration;
        WhenIframesActivatedGetDuration?.Invoke(duration);
    }
    [SerializeField] float hitIframesDuration = 2.25f;
    public override bool HasIframes
    {
        get
        {
            if (iframesEndTime == 0f)
            {
                return false;
            }
            if (iframesEndTime > Time.time)
            {
                return true;
            }
            return false;
        }
    }
    public float IframesDurationLeft => (iframesEndTime - Time.time).Max(0f);
    public void SendHit(IHit.HitPacket packet, out float damageDealt)
    {
        damageDealt = 0f;
        if (!StartHitRoutine())
        {
            return;
        }
        damageDealt = 500f;
    }
    bool StartHitRoutine()
    {
        if (currentHit != null)
        {
            return false;
        }
        if (HasIframes)
            return false;
        currentHit = GlobalCoroutineRunner.StartRoutine("Player Hit", CO_Hit(), false);
        IEnumerator CO_Hit()
        {
            bool hasSession = GameSession.CurrentAs(out ShmupSession session);
            IEnumerator KillAndRespawn()
            {
                manualAliveFlag = false;
                gameObject.SetActive(false);
                hitData.DeathSound.Play(CurrentPosition);
                int lifePool = session.GetInt(ShmupSession.keys.CurrentLives);
                bool canRespawn = lifePool > 0;
                if (lifePool > 0)
                {
                    lifePool--;
                    session.SetInt(ShmupSession.keys.CurrentLives, lifePool, 0, 6);
                }
                yield return 1f.WaitForSeconds();
                if (!canRespawn)
                {
                    if (session.CanContinue)
                    {
                        bool continued = false;
                        void Yes()
                        {
                            continued = true;
                            session.TryContinue();
                        }
                        void No()
                        {
                            session.ExitToMenu();
                        }
                        ContinueButtons.Show(out WaitUntil continueWait, Yes, No);
                        TimeSlowHandler.AddDurationlessTimescale("Continue Stall", 0f);
                        yield return continueWait;
                        TimeSlowHandler.RemoveDurationlessTimescale("Continue Stall");
                        if (!continued)
                            yield break;
                    }
                    else
                    {
                        yield return 1.5f.WaitForSeconds();
                        session.ExitToMenu();
                        yield break;
                    }
                }
                Vector2 v = new Vector2Shmup(0.5f, 0.2f).Vector2Now;
                manualAliveFlag = true;
                transform.position = v;
                gameObject.SetActive(true);
                SetCurrentIFrames(hitIframesDuration);
                int currentBombs = session.GetInt(ShmupSession.keys.CurrentBombs);
                session.SetInt(ShmupSession.keys.CurrentBombs, currentBombs.Max(session.GetInt(ShmupSession.keys.StartingBombs)), 0, 6);
            }
            bool cancelable = hasSession && session.GetInt(ShmupSession.keys.CurrentBombs) > 0;
            if (!cancelable)
            {
                yield return KillAndRespawn();
                currentHit = null;
                yield break;
            }
            hitData.HitVolume.weight = 1f;
            hitData.HitSound.Play(CurrentPosition);
            SetCurrentIFrames(hitIframesDuration);
            bool cancelled = false;
            float endUnscaled = 0.2f;
            while (endUnscaled > 0f && !cancelled)//cancelling window
            {
                TimeSlowHandler.AddSlow("HitCancel", 0.05f, 0.2f, 0f);
                if (GeneralManager.IsPaused)
                {
                    yield return null;
                    continue;
                }
                if (ShmupInput.BombJustPressed)
                {
                    cancelled = true;
                    //session.ChangeResource(ShmupSession.keys.CurrentBombs, -1);
                    //economy is paid for elsewhere
                }

                endUnscaled -= Time.unscaledDeltaTime;
                yield return null;
            }
            TimeSlowHandler.RemoveSlow("HitCancel");
            hitData.HitVolume.weight = 0f;
            if (!cancelled)//no cancel, death
            {
                yield return KillAndRespawn();
            }
            else
            {

            }
            currentHit = null;
        }
        return true;
    }
}
#endregion
public partial class ShmupPlayer : ShmupUnit
{
    [field: SerializeField] public Collider2D PlayerUnitCollider { get; private set; }
    [field: SerializeField] public ACWrapper SweepSound { get; private set; }
    [SerializeField] List<Collider2D> playerHitboxes = new List<Collider2D>();
    public override IEnumerable<Collider2D> Hitboxes
    {
        get
        {
            foreach (var item in playerHitboxes)
            {
                yield return item;
            }
        }
    }
    [SerializeField]
    Transform centerObject;
    public override Vector2 CurrentPosition => centerObject == null ? base.CurrentPosition : centerObject.position;
    private bool manualAliveFlag = true;
    [SerializeField] InputActionReference focusKey;
    [SerializeField] InputActionReference bombKey;
    public static bool BlockProjectileSpawning
    {
        get
        {
            return false;
        }
    }

    protected override bool CalculateAlive()
    {
        return manualAliveFlag;
    }
    protected override void WhenAwake()
    {
        base.WhenAwake();
        Player = this;
    }
    protected override void WhenDestroy()
    {
        base.WhenDestroy();
        if (Player == this)
            Player = null;
    }
    protected override void WhenStart()
    {
        base.WhenStart();
    }
    protected override void WhenUpdate()
    {
        if (GeneralManager.IsPaused)
            return;
        void MoveLoop()
        {
            Vector2 input = GenericInput.Move;
            IShmupMover.MoveSuccess moveResult = IShmupMover.MoveSuccess.Default;
            if (focusKey.IsPressed())
            {
                if (shmupMovers[1] is IShmupMover mover)
                {
                    mover.Move(this, input, out moveResult);
                    return;
                }
            }
            else
            {
                if (shmupMovers[0] is IShmupMover mover)
                {
                    mover.Move(this, input, out moveResult);
                    return;
                }
            }
        }
        if (bombKey.JustPressed())
        {
            PlayerBomb.TryTriggerBomb(this);
        }
        MoveLoop();
        ShmupWorldspace.MapToWorldspaceUnclamped(0.5f, 0.5f, out Vector2 space);
    }
}