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
    public class MusicNode : StageNode, IStageNodeRunWhenStart
    {
        public MusicWrapper music;
        protected override void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected)
        {
#if UNITY_EDITOR
            int index = 0;
            RecordUndo("Modify Node Value");
            music = EF_ObjectField(Helper_BuildFieldRect(rect, ref index), "Music", music);
#endif
        }
        public IEnumerator RunWhenStart()
        {
            Debug.Log("Play Music Node");
            music.Play();
            yield break;
        }
        protected override Vector2 BuildSize()
        {
            return new(500f, 60f);
        }
    }
}