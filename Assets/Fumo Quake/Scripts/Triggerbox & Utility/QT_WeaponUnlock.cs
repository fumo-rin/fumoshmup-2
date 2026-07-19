using rinCore;
using UnityEngine;

namespace FumoQuake
{
    [SelectionBase]
    public class QT_WeaponUnlock : QT_Base, IHierarchyComponentColor
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
