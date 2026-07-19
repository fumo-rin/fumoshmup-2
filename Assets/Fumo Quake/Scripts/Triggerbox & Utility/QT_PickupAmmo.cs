using UnityEngine;
using rinCore;

namespace FumoQuake
{
    public class QT_PickupAmmo : QT_Base, IHierarchyComponentColor
    {
        [SerializeField] QuakeWeaponItem unlock;
        public Color LabelColor => ColorHelper.PastelBlue.Opacity(50);
        protected override void WhenAwake()
        {

        }
        protected override bool WhenTriggerEnter(Collider other, IFumoUnit unit)
        {
            return PlayerWeaponsController.AwardAmmo((int)unlock, 0.125f, out float delta) && delta > 0f;
        }
    }
}
