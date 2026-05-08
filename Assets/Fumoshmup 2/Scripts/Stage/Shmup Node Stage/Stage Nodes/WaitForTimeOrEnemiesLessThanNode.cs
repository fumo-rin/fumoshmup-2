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
    public class WaitForTimeOrEnemiesAliveNode : StageNode, IStageNodeRunable
    {
        public float WaitDuration = 3f;
        public int AliveEnemiesRequired = 0;
        public IStageNodeModifier linkedModifier { get; set; }
        public bool RunSeperately => false;
        public float RunDuration => 0f;
        public bool WasModifiedByModifier { get; set; } = false;
        public bool IsLinkable => false;

        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {
#if UNITY_EDITOR
            int index = 0;
            RecordUndo("Modify Node Value");
            WaitDuration = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Wait Duration", WaitDuration, 0.05f, 15f);
            RecordUndo("Modify Node Value");
            AliveEnemiesRequired = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Alive Enemies", AliveEnemiesRequired, 0, 25);
#endif
        }
        public IEnumerator RunNode()
        {
            yield return StageTools.WaitForTimeOrEnemyCountLessThan(WaitDuration, AliveEnemiesRequired + 1);
        }
        protected override Vector2 BuildSize() => new(450f, 75f);
    }
}