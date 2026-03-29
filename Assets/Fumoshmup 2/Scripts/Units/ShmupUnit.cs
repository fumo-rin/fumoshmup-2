using FumoShmup2;
using rinCore;
using System.Collections.Generic;
using UnityEngine;

public abstract class ShmupUnit : FumoUnit
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
    protected override void WhenStart()
    {

    }
    protected override void WhenUpdate()
    {

    }
}
