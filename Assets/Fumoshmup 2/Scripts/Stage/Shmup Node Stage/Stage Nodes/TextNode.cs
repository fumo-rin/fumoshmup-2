using rinCore;
using System.Collections;
using UnityEngine;

namespace FumoShmup2
{
    public class TextNode : StageNode, IStageNodeRunable
    {
        public string containedMessage;
        public ShmupTextBuilderUI.textPacket TextPacket = new()
        {
            color = ColorHelper.White,
            duration = 3f,
            fontSize = 6,
            horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center,
            verticalAlignment = TMPro.VerticalAlignmentOptions.Middle,
            a01 = new(0.2f, 0.4f),
            b01 = new(0.8f, 0.6f)
        };

        public bool runSeperately;
        public float addedPostDelay = 0f;
        public bool RunSeperately => runSeperately;
        public float RunDuration => TextPacket.duration;
        public bool WasModifiedByModifier { get; set; } = false;
        public bool IsLinkable => false;
        public IEnumerator RunNode()
        {
            ShmupTextBuilderUI.CreateText(containedMessage, TextPacket);
            if (!RunSeperately)
            {
                yield return RunDuration.WaitForSeconds();
            }
            yield return addedPostDelay.WaitForSeconds();
        }

        protected override Vector2 BuildSize()
        {
#if UNITY_EDITOR
            return new(450f, 400f);
#else
            return new(0f, 0f);
#endif
        }

        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {
#if UNITY_EDITOR
            int index = 0;
            containedMessage = EF_Utility.EF_TextField(Helper_BuildFieldRect(in rect, ref index, 5), "Contained Message", containedMessage, 5);
            index++;
            TextPacket.a01.x = EF_Utility.EF_NumberField(Helper_BuildFieldRect(in rect, ref index, 1), "A x", TextPacket.a01.x, 0f, 1f);
            TextPacket.a01.y = EF_Utility.EF_NumberField(Helper_BuildFieldRect(in rect, ref index, 1), "A Y", TextPacket.a01.y, 0f, 1f);
            TextPacket.b01.x = EF_Utility.EF_NumberField(Helper_BuildFieldRect(in rect, ref index, 1), "B X", TextPacket.b01.x, 0f, 1f);
            TextPacket.b01.y = EF_Utility.EF_NumberField(Helper_BuildFieldRect(in rect, ref index, 1), "B Y", TextPacket.b01.y, 0f, 1f);
            TextPacket.duration = EF_Utility.EF_NumberField(Helper_BuildFieldRect(in rect, ref index, 1), "Duration", TextPacket.duration, 0.5f, 20f);
            TextPacket.fontSize = EF_Utility.EF_NumberField(Helper_BuildFieldRect(in rect, ref index, 1), "Font Size", TextPacket.fontSize, 2f, 72f);
            TextPacket.color = EF_Utility.EF_ColorField(Helper_BuildFieldRect(in rect, ref index, 1), "Color", TextPacket.color);
            TextPacket.horizontalAlignment = EF_Utility.EF_EnumDropdown(Helper_BuildFieldRect(in rect, ref index, 1), "Horizontal Alignment", TextPacket.horizontalAlignment);
            TextPacket.verticalAlignment = EF_Utility.EF_EnumDropdown(Helper_BuildFieldRect(in rect, ref index, 1), "Vertical Alignment", TextPacket.verticalAlignment);

            index++;
            addedPostDelay = EF_Utility.EF_NumberField(Helper_BuildFieldRect(in rect, ref index, 1), "Added Post Delay", addedPostDelay, 0f, 20f);
            runSeperately = EF_Utility.EF_BoolField(Helper_BuildFieldRect(in rect, ref index, 1), "Run Seperately", runSeperately);
#endif
        }
    }
}
