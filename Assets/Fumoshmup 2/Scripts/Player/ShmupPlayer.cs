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
    public void Sendhit(IHit.HitPacket packet, out float damageDealt)
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
        currentHit = StageRoutines.StartRoutine("Player Hit", CO_Hit(), true);
        IEnumerator CO_Hit()
        {
            bool hasSession = GameSession.CurrentAs(out ShmupSession session);
            IEnumerator KillAndRespawn()
            {
                manualAliveFlag = false;
                gameObject.SetActive(false);
                hitData.DeathSound.Play(CurrentPosition);
                yield return 1f.WaitForSeconds();
                Vector2 v = new Vector2Shmup(0.5f, 0.2f).Vector2Now;
                manualAliveFlag = true;
                transform.position = v;
                gameObject.SetActive(true);
                session.SetResource(ShmupSession.keys.CurrentBombs, session.GetResource(ShmupSession.keys.StartingBombs));
            }
            bool cancelable = hasSession && session.GetResource(ShmupSession.keys.CurrentBombs) > 0;
            if (!cancelable)
            {
                yield return KillAndRespawn();
                currentHit = null;
                yield break;
            }
            hitData.HitVolume.weight = 1f;
            hitData.HitSound.Play(CurrentPosition);
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
                    session.ChangeResource(ShmupSession.keys.CurrentBombs, -1);
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
    [SerializeField] ProjectileDefineSO testProjectile;
    int iteration = 0;
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
        MoveLoop();
        ShmupWorldspace.MapToWorldspaceUnclamped(0.5f, 0.5f, out Vector2 space);

        if (ShmupInput.BombJustPressed)
        {
            ProjectileRunner.TriggerSweep(0.5f, 255, true, out _);
        }

        var input = new Projectile.InputSettings(space, null, Vector2.down, new Projectile.ProjectileDamage(null, 10, 1), ProjectileFaction.Enemy);
        float offset = -iteration.AsFloat(0.1f) * iteration.AsFloat(0.1f);
        input.addedForward = 0.5f;

        for (int i = 0; i < 20; i++)
        {
            if (iteration % 2 == 0)
            {
                new Projectile.ArcSettings(0f + offset, 315f + offset, 45f, 10f + i.AsFloat(0.05f)).Spawn(input, testProjectile, out _);
            }
        }
        iteration += 1;
    }
}