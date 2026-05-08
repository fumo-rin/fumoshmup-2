using System.Collections;
using UnityEngine;
using UnityEngine.Search;
using rinCore;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Search;
using static rinCore.EF_Utility;
#endif
namespace FumoShmup2
{
    public class WaitForTimeNode : StageNode, IStageNodeRunable
    {
        public float WaitTime = 1f;
        public bool RunSeperately => false;
        public float RunDuration => 0f;
        public bool IsLinkable => false;
        public IStageNodeModifier linkedModifier { get; set; }
        public bool WasModifiedByModifier { get; set; } = false;
        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {
#if UNITY_EDITOR
            int index = 0;
            RecordUndo("Modify Node Value");
            WaitTime = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Wait Duration", WaitTime, 0.25f, 25f);
#endif
        }
        public IEnumerator RunNode()
        {
            yield return WaitTime.WaitForSeconds();
        }
        protected override Vector2 BuildSize()
        {
            return new Vector2(350f, 120f);
        }
    }
}