using rinCore;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FumoShmup2
{
    public abstract class ShmupPlayerShooter : MonoBehaviour
    {
        [SerializeField] int Dps_Unfocus = 600, Dps_Focus = 440;
        [SerializeField] InputActionReference shootingAction, focusAction, powerFire;
        [SerializeField] ShmupUnit Owner;
        [SerializeField] ProjectileDefineSO unfocusShot, optionShot, superShot;
        [SerializeField] ACWrapper unfocusShotSound;
        [SerializeField] Transform[] shotOptionNests4 = new Transform[4];
        AttackBuilder a = new();
        HashSet<Projectile> shotcap = new();
    }
}
