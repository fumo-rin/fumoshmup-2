using rinCore;
using UnityEngine;

namespace FumoQuake
{
    public class QT_PickupHealth : QT_Base, IHierarchyComponentColor
    {
        public Color LabelColor => ColorHelper.PastelBlue.Opacity(50);

        protected override void WhenAwake()
        {

        }

        protected override bool WhenTriggerEnter(Collider other, IFumoUnit unit)
        {
            if (IFumoUnit.Player is IFumoUnit player && player != null)
            {
                if (player.unitGameObject.TryGetComponent(out QuakeController c))
                {
                    if (c.HealthWithSideEffects >= 100)
                    {
                        return false;
                    }
                    c.HealthWithSideEffects += 25f;
                    return true;
                }
            }
            return false;
        }
    }
}
