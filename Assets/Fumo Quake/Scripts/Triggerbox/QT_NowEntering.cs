using rinCore;
using UnityEngine;

namespace FumoQuake
{
    public class QT_NowEntering : QT_Base
    {
        [SerializeField] string ZoneName;
        [SerializeField] GameXYTextDisplay.textPacket textPacket;
        static QT_NowEntering lastTouched;
        private void Awake()
        {
            lastTouched = null;
        }
        protected override void WhenTriggerEnter(Collider other, IFumoUnit unit)
        {
            if (lastTouched != this)
                GameXYTextDisplay.CreateText("Now Entering####".ReplaceLineBreaks("##") + ZoneName, textPacket);
            lastTouched = this;
        }
    }
    public abstract class QT_Base : MonoBehaviour
    {
        [SerializeField] public bool OnlyFindPlayer;
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.TryGetComponent(out IFumoUnit f) && (f.IsPlayer || !OnlyFindPlayer))
            {
                WhenTriggerEnter(other, f);
            }
        }
        protected abstract void WhenTriggerEnter(Collider other, IFumoUnit unit);
    }
}
