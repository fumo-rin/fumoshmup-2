using System.Collections;
using UnityEngine;
using UnityEngine.Search;
using rinCore;
using System.Collections.Generic;
using System.Linq;

namespace FumoShmup2
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.Search;
    using static rinCore.EF_Utility;
#endif
#if UNITY_EDITOR
    public static class StageNodeAssetHelper
    {
        public static void EnsureSubAssets(StageNode parentNode, IEnumerable<UnityEngine.Object> children)
        {
            if (parentNode == null || children == null) return;

            string path = AssetDatabase.GetAssetPath(parentNode);
            if (string.IsNullOrEmpty(path)) return;

            foreach (var child in children)
            {
                if (child == null) continue;
                if (!AssetDatabase.Contains(child))
                {
                    AssetDatabase.AddObjectToAsset(child, path);
                    EditorUtility.SetDirty(child);
                }
            }

            AssetDatabase.SaveAssets();
        }
    }
#endif
    #region Interfaces
    public interface IStageNodeModable
    {
        public EnemyModifierNode EnemyMod { get; set; }
    }
    public interface IStageNodeRunable
    {
        public bool RunSeperately { get; }
        public float RunDuration { get; }
        public bool WasModifiedByModifier { get; set; }
        public bool IsLinkable { get; }
        IEnumerator RunNode();
    }
    public interface IStageNodeRunWhenStart
    {
        IEnumerator RunWhenStart();
    }
    #endregion
    public abstract class StageNode : ScriptableObject
    {
        public bool IsEnabled;
        public string title = "Dummy Node";
        public Vector2 position;
        public int skipIndex;
        public Vector2 Size => BuildSize();
        protected abstract Vector2 BuildSize();

#if UNITY_EDITOR
        [System.NonSerialized]
        public UnityEngine.Object unityBackingObject;
        public bool IsCompacted;
#endif
        internal void DrawFromEditor(ShmupNodeStage stage, Rect rect, in bool selected)
        {
#if UNITY_EDITOR
            EditorGUI.BeginChangeCheck();
            if (IsCompacted)
            {
                DrawCompactedContents(stage, rect, selected);
            }
            else
            {
                DrawNodeContents(stage, rect, selected);
            }
            if (EditorGUI.EndChangeCheck())
            {
                stage.Dirty();
            }
#endif
        }
        protected virtual void DrawCompactedContents(ShmupNodeStage stage, Rect rect, in bool selected) { }
        protected abstract void DrawNodeContents(ShmupNodeStage stage, Rect rect, in bool selected);

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void EnsureGUIDOnDomainReload()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {

            };
        }
#endif
#if UNITY_EDITOR
        protected Rect Helper_BuildFieldRect(in Rect rect, ref int fieldNumber, int increments = 1)
        {
            Rect box = new Rect(rect.x + 10, rect.y + 30 + fieldNumber.AsFloat().Multiply(20f), rect.width - 20, 20 * increments);
            fieldNumber = fieldNumber + increments;
            return box;
        }
        protected Vector2 EF_ShmupSpace(Vector2 v, Color32 color, string label)
        {
            return ShmupStageEditor.EF_ShmupBox(v, color, label);
        }
        protected void RecordUndo(string actionName)
        {
            if (unityBackingObject != null)
                Undo.RecordObject(unityBackingObject, actionName);
        }
#endif
    }
}