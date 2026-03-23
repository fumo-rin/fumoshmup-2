using FumoShmup;
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
            hitData.HitVolume.weight = 1f;
            bool cancelable = true;
            if (cancelable)
            {
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
                        cancelled = true;

                    endUnscaled -= Time.unscaledDeltaTime;
                    yield return null;
                }
                TimeSlowHandler.RemoveSlow("HitCancel");
                hitData.HitVolume.weight = 0f;
                if (!cancelled)//no cancel, death
                {
                    manualAliveFlag = false;
                    gameObject.SetActive(false);
                    hitData.DeathSound.Play(CurrentPosition);
                    yield return 1f.WaitForSeconds();
                    manualAliveFlag = true;
                    gameObject.SetActive(true);
                }
                else
                {

                }
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
    IShmupMover[] playerMovers;
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
        playerMovers = new IShmupMover[2]
        {
            new PlayerShmupMover(11f),
            new PlayerShmupMover(6f)
        };
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
                if (playerMovers[1] is IShmupMover mover)
                {
                    mover.Move(this, input, out moveResult);
                    return;
                }
            }
            else
            {
                if (playerMovers[0] is IShmupMover mover)
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

        for (int i = 0; i < 1; i++)
        {
            if (iteration % 4 == 0)
            {

                new Projectile.ArcSettings(0f + offset, 315f + offset, 45f, 10f + i.AsFloat(0.5f)).Spawn(input, testProjectile, out _);
            }
        }
        iteration += 1;
    }
}