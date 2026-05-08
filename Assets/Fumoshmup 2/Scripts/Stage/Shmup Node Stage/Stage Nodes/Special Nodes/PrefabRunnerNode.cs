using rinCore;
using System.Collections;
using UnityEngine;

namespace FumoShmup2
{
    public class PrefabRunnerNode : StageNode, IStageNodeRunable
    {
        public StageRunablePrefab prefab;
        public bool CachedRunSeperately = false;
        public bool RunSeperately => CachedRunSeperately;
        public float RunDuration => 0f;
        public bool WasModifiedByModifier { get; set; }
        public bool IsLinkable => false;
        public IEnumerator RunNode()
        {
            yield return prefab.RunItem(CachedRunSeperately);
        }
        protected override Vector2 BuildSize()
        {
            return new(350f, 150f);
        }
        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {
#if UNITY_EDITOR
            int index = 0;
            prefab = EF_Utility.EF_ObjectField(Helper_BuildFieldRect(rect, ref index, 1), "Runable Item", prefab);
            CachedRunSeperately = EF_Utility.EF_BoolField(Helper_BuildFieldRect(rect, ref index, 1), "Run Seperately", CachedRunSeperately);
#endif
        }
    }
}
