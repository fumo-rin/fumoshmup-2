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
            if (SceneLoader.IsLoading)
                return false;
            other.transform.position += new Vector3(0f, 100f, 0);
            bool success;
            if (success = QuakeSession.CurrentAs(out QuakeSession ses) && ses.NextLevelOrMenu())
            {
                Destroy(this);
            }
            else
            {
                Debug.LogError("yuh..");
            }
            return true;
        }
    }
}
