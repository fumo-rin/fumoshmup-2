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
    public class RepeatNode : StageNode, IStageNodeModifier
    {
        [SerializeField]
        private List<StageNode> linkedNodes = new();
        public List<StageNode> LinkedNodes => linkedNodes;
        public int RepeatCount = 1;
        public float DelayBetweenRepeats = 0.25f;
        public bool SkipDelayForLastRepeat = true;
        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {
#if UNITY_EDITOR
            int index = 0;
            RecordUndo("Modify Node Value");
            RepeatCount = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Repeat Count", RepeatCount, 1, 25);
            RecordUndo("Modify Node Value");
            DelayBetweenRepeats = EF_Slider(Helper_BuildFieldRect(rect, ref index), "Delay Between Repeats", DelayBetweenRepeats, 0f, 10f);
            RecordUndo("Modify Node Value");
            SkipDelayForLastRepeat = EF_BoolField(Helper_BuildFieldRect(rect, ref index), "Skip Delay For Last Repeat", SkipDelayForLastRepeat);
            RecordUndo("Modify Node Value");
            if (EF_Button(Helper_BuildFieldRect(rect, ref index), "Start Linking"))
            {
                stage.LinkStart(this);
            }
#endif
        }
        protected override Vector2 BuildSize()
        {
            return new(350f, 130f);
        }
        public IEnumerator ModifyNode(IStageNodeRunable runable)
        {
            IEnumerator RoutineDelay(float delay, IEnumerator co)
            {
                yield return delay.WaitForSeconds();
                yield return co.MoveNext();
            }
            bool wasModified = runable.WasModifiedByModifier;
            runable.WasModifiedByModifier = true;
            for (int i = 0; i < RepeatCount; i++)
            {
                if (runable.RunSeperately)
                {
                    float delay = i == 0 && !wasModified ? 0f : DelayBetweenRepeats * i;
                    delay += runable.RunDuration * i;
                    GlobalCoroutineRunner.StartRoutine($"Stage Extras", RoutineDelay(delay, runable.RunNode()), false);
                }
                else
                {
                    IEnumerator nodeRoutine = runable.RunNode();
                    while (nodeRoutine.MoveNext())
                        yield return nodeRoutine.Current;

                    if (i == RepeatCount - 1 && SkipDelayForLastRepeat)
                        break;

                    yield return DelayBetweenRepeats.WaitForSeconds();
                }
            }
        }
    }
}