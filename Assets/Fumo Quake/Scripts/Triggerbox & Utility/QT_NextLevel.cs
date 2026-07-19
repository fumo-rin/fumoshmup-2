using rinCore;
using UnityEngine;

namespace FumoQuake
{
    [SelectionBase]
    public class QT_NextLevel : QT_Base
    {
        protected override void WhenAwake()
        {

        }
        protected override bool WhenTriggerEnter(Collider other, IFumoUnit unit)
        {
            other.transform.position += new Vector3(0f, 100f, 0);
            QuakeSession.NextLevelOrMenu();
            return true;
        }
    }
}
