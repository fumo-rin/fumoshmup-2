using rinCore;
using UnityEngine;

namespace FumoQuake
{
    public class QT_WeaponUnlock : QT_Base
    {
        enum UnlockItem
        {
            Blaster = 0,
            Shotgun = 1,
            Nailgun = 2,
            RocketLauncher = 3,
            LightningGun = 4,
            RailGun = 5,
        }
        [SerializeField] UnlockItem unlock;
        protected override void WhenTriggerEnter(Collider other, IFumoUnit unit)
        {
            PlayerWeaponsController.Unlock((int)unlock);
        }
    }
}
