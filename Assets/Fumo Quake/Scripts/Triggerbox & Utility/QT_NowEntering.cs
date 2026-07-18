using rinCore;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace FumoQuake
{
    [SelectionBase]
    public class QT_NowEntering : QT_Base
    {
        [SerializeField] string ZoneName;
        [SerializeField] GameXYTextDisplay.textPacket textPacket;
        static QT_NowEntering lastTouched;
        protected override void WhenAwake()
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
        [SerializeField] public bool DestroyOnCollect;
        [SerializeField] public List<Renderer> VisibleRenderers = new();
        protected abstract void WhenAwake();
        private void Awake()
        {
            foreach (var renderer in transform.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }
            foreach (var item in VisibleRenderers)
            {
                item.enabled = true;
            }
            WhenAwake();
        }
        private void OnTriggerEnter(Collider other)
        {
            if (SceneLoader.IsLoading || Time.timeScale == 0)
                return;
            if (other.transform.TryGetComponent(out IFumoUnit f) && (f.IsPlayer || !OnlyFindPlayer))
            {
                WhenTriggerEnter(other, f);
                if (DestroyOnCollect)
                    Destroy(gameObject);
            }
        }
        protected abstract void WhenTriggerEnter(Collider other, IFumoUnit unit);
    }
}
