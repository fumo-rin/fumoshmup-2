using rinCore;
using UnityEngine;

namespace FumoQuake
{
    enum QuakeWeaponItem
    {
        Blaster = 0,
        Shotgun = 1,
        Nailgun = 2,
        RocketLauncher = 3,
        LightningGun = 4,
        RailGun = 5,
    }
    [SelectionBase]
    public class QT_WeaponUnlock : QT_Base, IHierarchyComponentColor
    {
        [SerializeField] QuakeWeaponItem unlock;
        public Color LabelColor => ColorHelper.PastelBlue.Opacity(50);
        protected override void WhenAwake()
        {

        }
        protected override bool WhenTriggerEnter(Collider other, IFumoUnit unit)
        {
            return PlayerWeaponsController.UnlockPickup((int)unlock);
        }
    }
}
