using FumoShmup;
using rinCore;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShmupPlayer : ShmupUnit
{
    [SerializeField] Transform centerObject;
    public override Vector2 CurrentPosition => centerObject == null ? base.CurrentPosition : centerObject.position;
    private bool manualAliveFlag => true;
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
        ShmupWorldspace.MapToWorldspaceUnclamped(0.5f, 0.75f, out Vector2 space);
        for (int i = 0; i < 30; i++)
        {
            if (iteration % 2 == 0)
            {
                var input = new Projectile.InputSettings(space, null, Vector2.down, new Projectile.ProjectileDamage(null, 10, 1), ProjectileFaction.Enemy);
                float offset = -iteration.AsFloat(0.1f) * iteration.AsFloat(0.1f);

                new Projectile.ArcSettings(0f + offset, 315f + offset, 45f, 8f + i.AsFloat(0.05f)).Spawn(input, testProjectile, out _);
            }
        }
        iteration += 1;
    }
}