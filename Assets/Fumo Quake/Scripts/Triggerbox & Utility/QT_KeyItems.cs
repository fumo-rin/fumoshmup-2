using rinCore;
using UnityEngine;

namespace FumoQuake
{
    public enum QuakeKeyItems
    {
        SilverKeyOfDestiny,
        GoldenTicketKey,
    }
    public class QT_KeyItems : QT_Base
    {
        public QuakeKeyItems SelectedItem = QuakeKeyItems.SilverKeyOfDestiny;
        protected override void WhenAwake()
        {

        }
        protected override bool WhenTriggerEnter(Collider other, IFumoUnit unit)
        {
            return QuakeSession.AwardItem(SelectedItem);
        }
    }
}
