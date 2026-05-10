using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using rinCore;
using System.Linq;
using System;

namespace FumoShmup2
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.Callbacks;
    #region Open File
    public partial class ShmupStageEditor : EditorWindow
    {
        private const string EditorPrefsActiveStageKey = "ShmupStageEditor_ActiveStagePath";
        [MenuItem("Fumorin/Shmup Stage Editor")]
        public static void OpenWindow()
        {
            var window = GetOrCreateWindow();
            window.Focus();
        }
        public static ShmupStageEditor GetOrCreateWindow()
        {
            var window = GetWindow<ShmupStageEditor>("Shmup Stage Editor");
            window.Focus();
            return window;
        }
        public static class ShmupNodeStageOpener
        {
            [OnOpenAsset(1)]
            public static bool OpenShmupNodeStage(int instanceID, int line)
            {
                UnityEngine.Object obj = EditorUtility.EntityIdToObject(instanceID);

                if (obj is ShmupNodeStage stage)
                {
                    var window = ShmupStageEditor.GetOrCreateWindow();

                    if (stage != null) window.SetActiveStage(stage);

                    window.Repaint();
                    return true;
                }
                return false;
            }
        }
        private void SetActiveStage(ShmupNodeStage stage)
        {
            activeStage = stage;
            if (stage != null)
            {
                string path = AssetDatabase.GetAssetPath(stage);
                EditorPrefs.SetString(EditorPrefsActiveStageKey, path);
            }
        }
    }
    #endregion
    #region Actually save lol
    public class ShmupStageAutoSave : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (string path in paths)
            {
                var stage = AssetDatabase.LoadAssetAtPath<ShmupNodeStage>(path);
                if (stage != null)
                {
                    ForceSaveStage(stage);
                }
            }
            return paths;
        }
        private static void ForceSaveStage(ShmupNodeStage stage)
        {
            if (stage == null)
                return;

            string path = AssetDatabase.GetAssetPath(stage);
            if (string.IsNullOrEmpty(path))
                return;
            EditorUtility.SetDirty(stage);
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in subAssets)
            {
                if (obj != null && obj != stage)
                    EditorUtility.SetDirty(obj);
            }
        }
    }
    #endregion
    public partial class ShmupStageEditor : EditorWindow
    {
        #region Skip Panel
        public bool ShowWithSkipIndex(StageNode node)
        {
            if (activeStage == null)
            {
                return false;
            }
            if (node == null)
            {
                return false;
            }
            bool contains = false;
            if (node.skipIndex >= 0)
            {
                foreach (var item in activeStage.SkipEntries)
                {
                    if (item.skipValue == node.skipIndex)
                        contains = true;
                }
                if (contains)
                    return CurrentSkipValue == node.skipIndex;
            }
            if (activeStage.SkipEntries.Count > 0 && node.skipIndex < 0 || contains == false)
            {
                node.skipIndex = activeStage.SkipEntries[0].skipValue;
                EditorUtility.SetDirty(this);
                return false;
            }
            return true;
        }
        #endregion
        #region Shmup Box
        private Vector2 shmupBoxSize = new Vector2(400, 500);
        private static readonly float knobSize = 12f;
        public static Vector2 EF_ShmupBox(Vector2 pos, Color32 color, string label)
        {
            Event e = Event.current;
            Rect shmupRect = GetShmupRect();

            Vector2 pixelPos = NormalizedToScreen(pos, shmupRect);
            Rect knobRect = new Rect(pixelPos.x - knobSize * 0.5f, pixelPos.y - knobSize * 0.5f, knobSize, knobSize);

            EditorGUI.DrawRect(knobRect, color);
            EditorGUIUtility.AddCursorRect(knobRect, MouseCursor.MoveArrow);

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (knobRect.Contains(e.mousePosition) && e.button == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        e.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        pixelPos += e.delta;
                        pos = ScreenToNormalized(pixelPos, shmupRect);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID && e.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }

            Vector2 topLeftRelative = pixelPos - shmupRect.position;
            GUIStyle style = EditorStyles.boldLabel;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(shmupRect.x + topLeftRelative.x, shmupRect.y + topLeftRelative.y, 200, 20), label, style);

            return pos;
        }

        private static Rect GetShmupRect()
        {
            var editor = EditorWindow.GetWindow<ShmupStageEditor>();
            return new Rect(editor.viewOffset, editor.shmupBoxSize);
        }
        private static Vector2 NormalizedToScreen(Vector2 normalized, Rect shmupRect)
        {
            normalized.y = 1f - normalized.y;
            Vector2 local = (normalized - new Vector2(0.5f, 0.5f)) * new Vector2(shmupRect.width, shmupRect.height);
            local += shmupRect.center;
            return local;
        }
        private static Vector2 ScreenToNormalized(Vector2 screen, Rect shmupRect)
        {
            Vector2 local = screen - shmupRect.center;
            Vector2 norm = local / new Vector2(shmupRect.width, shmupRect.height) + new Vector2(0.5f, 0.5f);
            norm.y = 1f - norm.y;
            return norm;
        }
        private void DrawShmupBox()
        {
            Rect rect = new Rect(viewOffset, shmupBoxSize);

            Handles.BeginGUI();
            EditorGUI.DrawRect(rect, new Color(0f, 1f, 1f, 0.1f));
            Handles.color = Color.cyan;
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, Color.cyan);
            Handles.EndGUI();
        }
        #endregion
        #region Context Menu
        private void DuplicateNode(StageNode original)
        {
            if (original == null || activeStage == null)
                return;
            Undo.RecordObject(activeStage, "Duplicate Node");
            StageNode newNode = ScriptableObject.CreateInstance(original.GetType()) as StageNode;
            if (newNode == null)
            {
                Debug.LogError($"Failed to duplicate node of type {original.GetType()}");
                return;
            }

            string json = JsonUtility.ToJson(original);
            JsonUtility.FromJsonOverwrite(json, newNode);

            newNode.position += new Vector2(330f, 30f);
            newNode.title = original.title;
            newNode.skipIndex = original.skipIndex;
            newNode.name = original.name + "_Copy";
            newNode.IsEnabled = original.IsEnabled;

            AssetDatabase.AddObjectToAsset(newNode, activeStage);
            activeStage.nodes.Add(newNode);
            EditorUtility.SetDirty(newNode);
            EditorUtility.SetDirty(activeStage);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Repaint();
        }
        private StageNode GetHoveredNode(Vector2 mousePosition)
        {
            StageNode hoveredNode = null;
            foreach (var node in activeStage.nodes)
            {
                if (node.skipIndex != CurrentSkipValue)
                    continue;
                Rect rect = new Rect(node.position + viewOffset, node.Size);
                if (rect.Contains(mousePosition))
                {
                    hoveredNode = node;
                    break;
                }
            }
            return hoveredNode;
        }
        private void ShowContextMenu(Vector2 mousePosition)
        {
            GenericMenu menu = new GenericMenu();
            StageNode hoveredNode = null;

            foreach (var node in activeStage.nodes)
            {
                if (node.skipIndex != CurrentSkipValue)
                    continue;
                Rect rect = new Rect(node.position + viewOffset, node.Size);
                if (rect.Contains(mousePosition))
                {
                    hoveredNode = node;
                    break;
                }
            }
            if (hoveredNode != null)
            {
                menu.AddItem(new GUIContent($"Delete \"{hoveredNode.title}\""), false, () =>
                {
                    activeStage.nodes.Remove(hoveredNode);
                    CleanUp(hoveredNode);
                    DestroyImmediate(hoveredNode);
                    EditorUtility.SetDirty(activeStage);
                    Repaint();
                });
                menu.AddItem(new GUIContent($"{(hoveredNode.IsEnabled ? "Disable" : "Enable")} {hoveredNode.title}"), false, () =>
                {
                    hoveredNode.IsEnabled = !hoveredNode.IsEnabled;
                    CleanUp(hoveredNode);
                    EditorUtility.SetDirty(activeStage);
                    Repaint();
                });
                foreach (var skip in activeStage.SkipEntries)
                {
                    menu.AddItem(new GUIContent($"Move To Stage Section/\"{skip.skipName}\""), false, () =>
                    {
                        hoveredNode.skipIndex = skip.skipValue;
                        EditorUtility.SetDirty(activeStage);
                        Repaint();
                    });
                }
                menu.AddItem(new GUIContent($"Duplicate \"{hoveredNode.title}\""), false, () =>
                {
                    DuplicateNode(hoveredNode);
                    EditorUtility.SetDirty(activeStage);
                    Repaint();
                });
                menu.AddItem(new GUIContent("Break Modifier Links"), false, () =>
                {
                    if (hoveredNode is IStageNodeRunable runable)
                    {
                        activeStage.BreakAllLinksToThis(hoveredNode);
                    }
                    if (hoveredNode is IStageNodeModifier mod)
                    {
                        mod.LinkedNodes.Clear();
                    }
                    if (hoveredNode is EnemyModifierNode enemyMod)
                    {
                        enemyMod.LinkedNodes.Clear();
                    }
                    EditorUtility.SetDirty(activeStage);
                    Repaint();
                });
            }
            else
            {
                ContextMenuCreateNodes(menu, mousePosition);
            }

            menu.ShowAsContext();
        }
        #endregion
        #region Draw Tools & Grid
        #region Helpers
        private Vector2[] GetClosestEdgePoint(Rect from, Rect to)
        {
            Vector2 fromCenter = from.center;
            Vector2 toCenter = to.center;

            Vector2 dir = (toCenter - fromCenter).normalized;

            float fromHalfX = from.width * 0.5f;
            float fromHalfY = from.height * 0.5f;
            Vector2 localDirFrom = new Vector2(
                Mathf.Clamp(dir.x, -1f, 1f),
                Mathf.Clamp(dir.y, -1f, 1f)
            );
            float scaleXFrom = fromHalfX / Mathf.Abs(localDirFrom.x);
            float scaleYFrom = fromHalfY / Mathf.Abs(localDirFrom.y);
            float scaleFrom = Mathf.Min(scaleXFrom, scaleYFrom);
            Vector2 fromEdge = fromCenter + dir * scaleFrom;

            Vector2 toDir = -dir;
            float toHalfX = to.width * 0.5f;
            float toHalfY = to.height * 0.5f;
            Vector2 localDirTo = new Vector2(
                Mathf.Clamp(toDir.x, -1f, 1f),
                Mathf.Clamp(toDir.y, -1f, 1f)
            );
            float scaleXTo = toHalfX / Mathf.Abs(localDirTo.x);
            float scaleYTo = toHalfY / Mathf.Abs(localDirTo.y);
            float scaleTo = Mathf.Min(scaleXTo, scaleYTo);
            Vector2 toEdge = toCenter + toDir * scaleTo;

            return new Vector2[] { fromEdge, toEdge };
        }
        private void DrawArrow(Vector2 start, Vector2 end, float arrowHeadSize = 12f, Color color = default)
        {
            if (color == default) color = Color.green;

            Handles.color = color;
            Handles.DrawLine(start, end);

            Vector2 direction = (end - start).normalized;
            Vector2 right = new Vector2(-direction.y, direction.x);

            Vector2 arrowBase = end - direction * arrowHeadSize;
            Vector2 p1 = arrowBase + right * arrowHeadSize * 0.5f;
            Vector2 p2 = arrowBase - right * arrowHeadSize * 0.5f;

            Handles.DrawAAConvexPolygon(end, p1, p2);
        }
        #endregion

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (activeStage == null)
            {
                string lastPath = EditorPrefs.GetString(EditorPrefsActiveStageKey, string.Empty);
                if (!string.IsNullOrEmpty(lastPath))
                {
                    var possibleStage = AssetDatabase.LoadAssetAtPath<ShmupNodeStage>(lastPath);
                    if (possibleStage != null)
                    {
                        if (GUILayout.Button("Reopen Last Stage", EditorStyles.toolbarButton))
                        {
                            SetActiveStage(possibleStage);
                            Debug.Log($"Reopened last stage: {possibleStage.name}");
                            Repaint();
                        }
                    }
                    else
                    {
                        EditorPrefs.DeleteKey(EditorPrefsActiveStageKey);
                    }
                }
            }
            ShmupNodeStage newStage = (ShmupNodeStage)EditorGUILayout.ObjectField(
                activeStage,
                typeof(ShmupNodeStage),
                false,
                GUILayout.Width(250)
            );
            if (newStage != activeStage)
                SetActiveStage(newStage);
            if (GUILayout.Button("Recenter View", EditorStyles.toolbarButton))
                CenterView();
            GUI.enabled = activeStage != null;
            if (GUILayout.Button("Force Save", EditorStyles.toolbarButton))
                ForceSaveActiveStage();
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        private void ForceSaveActiveStage()
        {
            if (activeStage == null)
            {
                Debug.LogWarning("No active stage to save.");
                return;
            }

            EditorUtility.SetDirty(activeStage);
            string path = AssetDatabase.GetAssetPath(activeStage);

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in subAssets)
            {
                if (obj != null)
                    EditorUtility.SetDirty(obj);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();

            Debug.Log($"Force saved: {activeStage.name} and all sub-assets at {path}");
        }
        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
        {
            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            float offsetX = viewOffset.x % gridSpacing;
            float offsetY = viewOffset.y % gridSpacing;

            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

            for (int i = 0; i <= widthDivs; i++)
                Handles.DrawLine(new Vector3(i * gridSpacing + offsetX, 0), new Vector3(i * gridSpacing + offsetX, position.height));
            for (int j = 0; j <= heightDivs; j++)
                Handles.DrawLine(new Vector3(0, j * gridSpacing + offsetY), new Vector3(position.width, j * gridSpacing + offsetY));

            Handles.color = Color.white;
            Handles.EndGUI();
        }
        private void DrawSkipEntries()
        {
            if (activeStage == null || activeStage.SkipEntries == null || activeStage.SkipEntries.Count == 0)
                return;

            float panelWidth = 150f;
            float entryHeight = 25f;
            int count = activeStage.SkipEntries.Count;
            float panelHeight = count * entryHeight + 10f;

            Vector2 panelPos = new Vector2(10f, position.height - panelHeight - 10f);
            Rect panelRect = new Rect(panelPos, new Vector2(panelWidth, panelHeight));

            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            GUI.Box(panelRect, GUIContent.none, EditorStyles.helpBox);
            GUI.color = Color.white;

            Rect innerRect = new Rect(panelRect.x + 5, panelRect.y + 5, panelRect.width - 10, entryHeight);
            for (int i = 0; i < count; i++)
            {
                var skip = activeStage.SkipEntries[i];
                int skipIndex = i;

                if (i == activeStage.selectedSkipIndex)
                    EditorGUI.DrawRect(innerRect, new Color(0.2f, 0.4f, 0.8f, 0.5f));

                GUI.color = skip.enabled ? Color.white : Color.gray;
                GUI.Box(innerRect, $"{skip.skipName}: {skip.skipValue}");
                GUI.color = Color.white;

                Event e = Event.current;
                if (innerRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        activeStage.selectedSkipIndex = skipIndex;
                        GUI.changed = true;
                        Repaint();
                        e.Use();
                    }
                    if (e.type == EventType.MouseDown && e.button == 1)
                    {
                        GenericMenu menu = new GenericMenu();
                        foreach (var targetSkip in activeStage.SkipEntries)
                        {
                            var targetSkipCopy = targetSkip;
                            var skipCopy = skip;

                            menu.AddItem(
                                new GUIContent($"Move All Nodes/\"{targetSkipCopy.skipName}\""),
                                false,
                                () =>
                                {
                                    int sourceSkipValue = skipCopy.skipValue;
                                    int targetSkipValue = targetSkipCopy.skipValue;

                                    activeStage.nodes.Where(n => n.skipIndex == sourceSkipValue).ToList().ForEach(n => n.skipIndex = targetSkipValue);

                                    EditorUtility.SetDirty(activeStage);
                                    Repaint();
                                });
                        }
                        menu.AddItem(
                            new GUIContent($"{(skip.enabled ? "Disable" : "Enable")} {skip.skipName}"),
                            false,
                            () =>
                            {
                                var item = activeStage.SkipEntries[skipIndex];
                                item.enabled = !item.enabled;
                                activeStage.SkipEntries[skipIndex] = item;

                                EditorUtility.SetDirty(activeStage);
                                Repaint();
                            });

                        menu.ShowAsContext();
                        e.Use();
                    }
                }

                innerRect.y += entryHeight;
            }

            EditorGUIUtility.AddCursorRect(panelRect, MouseCursor.Arrow);
        }
        #endregion
        #region Draw Nodes
        private void DrawNodeConnection(StageNode fromNode, StageNode toNode)
        {
            if (fromNode == null || toNode == null)
                return;

            Rect fromRect = new Rect(fromNode.position + viewOffset, fromNode.Size);
            Rect toRect = new Rect(toNode.position + viewOffset, toNode.Size);

            Vector2[] points = GetClosestEdgePoint(fromRect, toRect);
            DrawArrow(points[0], points[1]);
        }
        private void DrawNodes()
        {
            void RefreshLinks()
            {
                if (activeStage == null) return;

                foreach (var mod in activeStage.nodes.OfType<IStageNodeModifier>())
                {
                    mod.RevalidateNodes();
                }
                foreach (var enemyMod in activeStage.nodes.OfType<EnemyModifierNode>())
                {
                    enemyMod.RevalidateNodes();
                }
            }
            if (activeStage == null) return;
            RefreshLinks();

            foreach (var node in activeStage.nodes)
            {
                if (node == null)
                {
                    Debug.LogWarning("Bad Nodes for : " + activeStage.name);
                    continue;
                }
                node.unityBackingObject = node;
                if (!ShowWithSkipIndex(node))
                    continue;

                Rect rect = new Rect(node.position + viewOffset, node.Size);
                Color prevColor = GUI.color;

                #region Draw Links
                if (node is IStageNodeModifier mod)
                {
                    mod.RevalidateNodes();
                    foreach (var item in mod.LinkedNodes)
                        DrawNodeConnection(mod as StageNode, item);
                }
                if (node is EnemyModifierNode enemyModLink)
                {
                    enemyModLink.RevalidateNodes();
                    foreach (var item in enemyModLink.LinkedNodes)
                        DrawNodeConnection(enemyModLink, item);
                }
                #endregion
                if (activeNode != node &&
                    (rect.xMax < -1000 || rect.yMax < -1000 ||
                     rect.xMin > position.width + 1000 ||
                     rect.yMin > position.height + 1000))
                    continue;

                bool isActive = node == activeNode;
                if (!node.IsEnabled)
                    GUI.color = ColorHelper.Gray3;
                else
                    GUI.color = isActive ? new Color(0.5f, 0.7f, 1f, 1f) : Color.white;

                string extraText = "";
                if (activeStage.IsLinking && ((node is IStageNodeRunable linkable && linkable.IsLinkable) || node is EnemyModifierNode))
                {
                    GUI.color = ColorHelper.PastelYellow;
                    extraText += "(Linkable)";
                }
                GUI.Box(rect, extraText + node.title.SpaceByCapitals());
                GUI.color = prevColor;
                node.DrawFromEditor(activeStage, rect, isActive);
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.MoveArrow);
            }
            GUI.color = Color.white;
        }
        #endregion
        #region Click Event
        private void HandleLink(Event e)
        {
            if (activeStage.IsLinking)
            {
                if (e.type == EventType.MouseDown && e.button == 0 && GetHoveredNode(e.mousePosition) is StageNode hoverLink && hoverLink is IStageNodeRunable runable)
                {
                    activeStage.LinkEnd(hoverLink);
                    activeStage.currentLinkAttempt = null;
                    e.Use();
                }
                if (e.type == EventType.MouseUp)
                {
                    activeStage.currentLinkAttempt = null;
                }
            }
        }
        private void HandleEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        StageNode clickedNode = GetHoveredNode(e.mousePosition);
                        if (clickedNode != null)
                        {
                            activeNode = clickedNode;
                            isDraggingNode = true;
                        }
                        else
                        {
                            activeNode = null;
                            isDraggingNode = false;
                        }

                        Repaint();
                        if (clickedNode != null)
                        {
                            e.Use();
                        }
                    }
                    else if (e.button == 1)
                    {
                        ShowContextMenu(e.mousePosition);
                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (e.button == 0)
                        isDraggingNode = false;
                    break;

                case EventType.MouseDrag:
                    if (e.button == 2)
                    {
                        viewOffset += e.delta;
                        Repaint();
                        e.Use();
                    }
                    else if (e.button == 0 && isDraggingNode && activeNode != null)
                    {
                        activeNode.position += e.delta;
                        Repaint();
                        e.Use();
                    }
                    break;
            }
        }
        #endregion
        #region Cleanup Unused nodes
        private void CleanUp(StageNode removed)
        {
            if (activeStage == null) return;
            string stagePath = AssetDatabase.GetAssetPath(activeStage);
            StageNode[] allNestedNodes = AssetDatabase.LoadAllAssetsAtPath(stagePath).OfType<StageNode>().ToArray();
            foreach (var node in allNestedNodes)
            {
                if (node == null) continue;
                if (!activeStage.nodes.Contains(node))
                {
                    Debug.Log($"Removing unused node: {node.title}");
                    AssetDatabase.RemoveObjectFromAsset(node);
                }
                if (string.IsNullOrEmpty(node.name))
                {
                    node.name = node.GetType().Name;
                }
                if (node is IStageNodeModifier modItem)
                {
                    modItem.LinkedNodes.Remove(node);
                    modItem.RevalidateNodes();
                }
                if (node is EnemyModifierNode enemyMod)
                {
                    enemyMod.LinkedNodes.Remove(removed);
                    enemyMod.RevalidateNodes();
                }
            }
            EditorUtility.SetDirty(activeStage);
            AssetDatabase.Refresh();
        }
        #endregion
        #region Context Menu Create Nodes
        private void ContextMenuCreateNodes(GenericMenu menu, Vector2 mousePosition)
        {
            AddCreateNodeEntries(menu, mousePosition, "Stage", typeof(LineSpawnerNode), typeof(SingleSpawnerNode));
            AddCreateNodeEntries(menu, mousePosition, "Boss", typeof(BossNode));
            AddCreateNodeEntries(menu, mousePosition, "Special", typeof(PrefabRunnerNode));
            AddCreateNodeEntries(menu, mousePosition, "Wait Instructions", typeof(WaitForTimeOrEnemiesAliveNode), typeof(DialogueAndWaitNode), typeof(WaitForTimeNode));
            AddCreateNodeEntries(menu, mousePosition, "When Section Start", typeof(MusicNode));
            AddCreateNodeEntries(menu, mousePosition, "Modifier Nodes", typeof(RepeatNode), typeof(EnemyModifierNode));
            AddCreateNodeEntries(menu, mousePosition, "Deprecated", typeof(BossNodeCave));
        }
        #region Special Nodes Helper
        void AddCreateNodeEntries(GenericMenu menu, Vector2 mousePosition, string entryCategory, params Type[] nodeTypes)
        {
            foreach (var type in nodeTypes)
            {
                if (!typeof(StageNode).IsAssignableFrom(type))
                    continue;

                string entryName = $"Create {entryCategory} Node/{type.Name}";
                menu.AddItem(new GUIContent(entryName), false, () =>
                {
                    AddNodeByType(type, mousePosition);
                });
            }
        }
        void AddNodeByType(Type nodeType, Vector2 mousePosition)
        {
            if (activeStage == null)
                return;

            Undo.RecordObject(activeStage, "Add Stage Node");
            Vector2 graphPos = mousePosition - viewOffset;
            StageNode node = ScriptableObject.CreateInstance(nodeType) as StageNode;
            if (node == null)
            {
                Debug.LogError($"Failed to create node of type {nodeType}");
                return;
            }

            node.position = graphPos;
            node.title = nodeType.Name;
            node.skipIndex = CurrentSkipValue;
            node.name = nodeType.Name;
            node.IsEnabled = true;

            AssetDatabase.AddObjectToAsset(node, activeStage);
            activeStage.nodes.Add(node);
            EditorUtility.SetDirty(node);
            EditorUtility.SetDirty(activeStage);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Repaint();
        }
        #endregion
        #endregion
        private void OnLostFocus()
        {
            activeNode = null;
            isDraggingNode = false;
            Repaint();
        }
        private void OnGUI()
        {
            DrawToolbar();
            if (activeStage == null)
            {
                EditorGUILayout.HelpBox("Select a FumoShmupStage asset to edit.", MessageType.Info);
                return;
            }
            DrawShmupBox();
            DrawGrid(GRID_SMALL, 0.2f, Color.gray);
            DrawGrid(GRID_LARGE, 0.4f, Color.gray);
            DrawNodes();
            DrawSkipEntries();
            if (activeStage.IsLinking)
            {
                HandleLink(Event.current);
            }
            else
            {
                HandleEvents(Event.current);
            }
        }
        private void OnDisable()
        {
            if (activeStage != null)
            {
                string path = AssetDatabase.GetAssetPath(activeStage);
                EditorPrefs.SetString(EditorPrefsActiveStageKey, path);
                activeStage = null;
            }
        }
        int CurrentSkipValue => activeStage == null ? -1 : activeStage.SkipEntries[activeStage.selectedSkipIndex % activeStage.SkipEntries.Count].skipValue;
        private const float GRID_SMALL = 20f;
        private const float GRID_LARGE = 100f;

        private Vector2 viewOffset = Vector2.zero;
        private ShmupNodeStage activeStage;
        private StageNode activeNode;
        private bool isDraggingNode;
        private void SelectNodeAt(Vector2 mousePos)
        {
            activeNode = null;
            isDraggingNode = false;

            for (int i = activeStage.nodes.Count - 1; i >= 0; i--)
            {
                var node = activeStage.nodes[i];
                Rect rect = new Rect(node.position + viewOffset, node.Size);
                if (rect.Contains(mousePos))
                {
                    activeNode = node;
                    isDraggingNode = true;
                    Repaint();
                    break;
                }
            }
        }
        private void CenterView()
        {
            if (activeStage == null || activeStage.nodes.Count == 0) return;

            Rect bounds = new Rect(activeStage.nodes[0].position, activeStage.nodes[0].Size);
            foreach (var node in activeStage.nodes.Where(x => x.skipIndex == CurrentSkipValue))
            {
                Rect r = new Rect(node.position, node.Size);
                bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, r.xMin), Mathf.Min(bounds.yMin, r.yMin),
                                          Mathf.Max(bounds.xMax, r.xMax), Mathf.Max(bounds.yMax, r.yMax));
            }
            Vector2 graphCenter = bounds.center;
            Vector2 screenCenter = new Vector2(position.width / 2f, position.height / 2f);
            viewOffset = screenCenter - graphCenter;
        }
    }
#endif

    [CreateAssetMenu(menuName = "FumoShmup2/Fumo Node Stage")]
    public class ShmupNodeStage : ShmupStage
    {
        #region Linking
#if UNITY_EDITOR
        public StageNode currentLinkAttempt;
        public bool IsLinking => currentLinkAttempt != null;
        public void LinkStart(StageNode start)
        {
            Debug.Log("Started Linking");
            currentLinkAttempt = null;
            if (start is IStageNodeModifier || start is IStageNodeRunable || start is EnemyModifierNode)
            {
                currentLinkAttempt = start;
            }
        }
        public void LinkEnd(StageNode end)
        {
            Debug.Log("Ending Link With : " + (currentLinkAttempt == null ? "Nothing" : currentLinkAttempt.title.ToString()) + " : " + (end == null ? "Nothing" : end.title.ToString()));
            if (currentLinkAttempt == end)
            {
                currentLinkAttempt = null;
                return;
            }
            IStageNodeModifier modifier = currentLinkAttempt as IStageNodeModifier ?? end as IStageNodeModifier;
            IStageNodeRunable runable = end as IStageNodeRunable ?? currentLinkAttempt as IStageNodeRunable;
            if (modifier != null && runable != null)
            {
                modifier.LinkNode(runable as StageNode);
            }
            if (currentLinkAttempt is EnemyModifierNode enemyMod && runable != null)
            {
                enemyMod.LinkNode(this, runable as StageNode);
            }
            currentLinkAttempt = null;
            this.SetDirtyAndSave();
        }
        public void BreakAllLinksToThis(StageNode breaker)
        {
            if (breaker == null) return;
            foreach (var modifier in nodes.Where(n => n != null).OfType<IStageNodeModifier>())
            {
                modifier.UnlinkNode(breaker);
            }
            foreach (var modifier in nodes.Where(n => n != null).OfType<EnemyModifierNode>())
            {
                modifier.LinkedNodes.Remove(breaker);
            }
        }
#endif
        #endregion
        [field: SerializeField] public int selectedSkipIndex = -1;
        [SerializeField] DialogueStackSO StageEndDialogue;
        public List<EnemyUnit> enemyTable = new();
        [SerializeReference]
        public List<StageNode> nodes = new List<StageNode>();
        public List<MusicWrapper> NodeMusic => nodes == null ? new() :
        nodes.Where(n => n != null).OrderBy(n => n.skipIndex).ThenBy(n => n.position.y).Select(n =>
        {
            return n switch
            {
                MusicNode musicNode => musicNode.music,
                BossNode bossNode => bossNode.bossMusic,
                _ => null
            };
        })
        .Where(m => m != null)
        .ToList();
        protected override IEnumerator StagePayload(int skip)
        {
            yield return 0.15f.WaitForSeconds();
            foreach (var skips in SkipEntries.Where(n => n.skipValue > skip && n.enabled).OrderBy(n => n.skipValue))
            {
                yield return CollectAndRunSkip(skips.skipValue);
            }
            yield return StageTools.WaitForTimeOrEnemyCountLessThan(999f, 1);
            StartDialogue(StageEndDialogue, out WaitUntil w, null);
            yield return w;
            yield return 1.5f.WaitForSeconds();
            if (ShmupSession.CurrentAs(out ShmupSession sess))
            {
                sess.LoadNextStageOrMenu();
            }
        }
        private IEnumerator CollectAndRunSkip(int skip)
        {
            var orderedNodes = nodes.Where(n => n != null && n.skipIndex == skip && n.IsEnabled)
                .OrderByDescending(n => n is IStageNodeRunWhenStart)
                .ThenBy(n => n.position.y)
                .ToList();

            HashSet<StageNode> alreadyRun = new();

            foreach (var node in orderedNodes)
            {
                if (node is IStageNodeRunWhenStart whenStart)
                {
                    alreadyRun.Add(node);
                    yield return whenStart.RunWhenStart();
                    continue;
                }
                if (node is IStageNodeModifier mod)
                {
                    alreadyRun.Add(node);

                    var linkedNodes = mod.LinkedNodes
                        .Where(n => n != null && n.skipIndex == skip)
                        .OrderBy(n => n.position.y)
                        .OfType<IStageNodeRunable>()
                        .ToList();

                    foreach (var modRunable in linkedNodes)
                    {
                        alreadyRun.Add(modRunable as StageNode);
                        yield return mod.ModifyNode(modRunable);
                    }
                    continue;
                }
                if (node is IStageNodeRunable runable && !alreadyRun.Contains(node))
                {
                    alreadyRun.Add(node);
                    yield return runable.RunNode();
                }
            }
        }
    }
}