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
    public class DialogueAndWaitNode : StageNode, IStageNodeRunable
    {
        public DialogueStackSO containedDialogue;
        public float delay = 0.25f, postDelay = 0.25f;
        public IStageNodeModifier linkedModifier { get; set; }
        public bool RunSeperately => false;
        public bool IsLinkable => false;
        public float RunDuration => 0f;
        public bool WasModifiedByModifier { get; set; } = false;
        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {
#if UNITY_EDITOR
            int index = 0;
            RecordUndo("Modify Node Value");
            containedDialogue = EF_ObjectField(Helper_BuildFieldRect(rect, ref index), "Dialogue", containedDialogue);
            RecordUndo("Modify Node Value");
            delay = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Wait Before Dialogue", delay, 0f, 5f);
            RecordUndo("Modify Node Value");
            postDelay = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Wait After Dialogue", postDelay, 0f, 5f);
#endif
        }
        public IEnumerator RunNode()
        {
            yield return delay.WaitForSeconds();
            StageTools.StartDialogue(containedDialogue, out WaitUntil w, null);
            yield return w;
        }
        protected override Vector2 BuildSize()
        {
            return new Vector2(350f, 120f);
        }
    }
}