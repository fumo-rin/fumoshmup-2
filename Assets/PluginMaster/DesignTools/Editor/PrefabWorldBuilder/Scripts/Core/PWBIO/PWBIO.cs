/*
Copyright (c) Omar Duarte
Unauthorized copying of this file, via any medium is strictly prohibited.
Writen by Omar Duarte.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System.Linq;
using UnityEngine;

namespace PluginMaster
{
    [UnityEditor.InitializeOnLoad]
    public static partial class PWBIO
    {
        #region HANDLERS AND EVENTS
        private const float TAU = Mathf.PI * 2;
        private static int _controlId;
        public static int controlId { set => _controlId = value; }
        private static ToolController.Tool tool => ToolController.current;

        private static UnityEditor.Tool _unityCurrentTool = UnityEditor.Tool.None;

        private static Camera _sceneViewCamera = null;

        public static bool repaint { get; set; }

        static PWBIO()
        {
            LineData.SetNextId();
            SelectionManager.selectionChanged += UpdateSelection;
            UnityEditor.Undo.undoRedoPerformed += OnUndoPerformed;
            UnityEditor.SceneView.duringSceneGui += DuringSceneGUI;
            PaletteManager.OnPaletteChanged += OnPaletteChanged;
            PaletteManager.OnBrushSelectionChanged += OnBrushSelectionChanged;
            ToolController.OnToolModeChanged += OnEditModeChanged;
#if UNITY_2021_1_OR_NEWER
            UnityEditor.SceneManagement.PrefabStage.prefabStageOpened += OnPrefabStageChanged;
            UnityEditor.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageChanged;
#endif
            UnityEditor.EditorApplication.delayCall += () =>
            {
                LineInitializeOnLoad();
                ShapeInitializeOnLoad();
                TilingInitializeOnLoad();
                FloorInitializeOnLoad();
                WallInitializeOnLoad();
#if PWB_BLOCK
                BlockInitializeOnLoad();
#endif
            };
        }

        private static void OnPaletteChanged()
        {
            ApplySelectionFilters();
            switch (ToolController.current)
            {
                case ToolController.Tool.ERASER:
                    if (EraserManager.settings.command == ISelectionBrushTool.Command.SELECT_PALETTE_PREFABS)
                        UpdateOctree();
                    break;
                case ToolController.Tool.REPLACER:
                    if (ReplacerManager.settings.command == ISelectionBrushTool.Command.SELECT_PALETTE_PREFABS)
                        UpdateOctree();
                    BrushstrokeManager.ClearReplacerDictionary();
                    break;
                case ToolController.Tool.CIRCLE_SELECT:
                    if (CircleSelectManager.settings.command == ISelectionBrushTool.Command.SELECT_PALETTE_PREFABS)
                        UpdateOctree();
                    break;
            }
        }

        private static void OnBrushSelectionChanged()
        {
            switch (ToolController.current)
            {
                case ToolController.Tool.GRAVITY:
                    InitializeGravityTool();
                    break;
                case ToolController.Tool.LINE:
                    ClearLineStroke();
                    break;
                case ToolController.Tool.SHAPE:
                    ClearShapeStroke();
                    break;
                case ToolController.Tool.TILING:
                    ClearTilingStroke();
                    break;
                case ToolController.Tool.SELECTION:
                    InitializeSelectionToolOnBrushChanged();
                    break;
                case ToolController.Tool.ERASER:
                    if (EraserManager.settings.command == ISelectionBrushTool.Command.SELECT_BRUSH_PREFABS)
                        UpdateOctree();
                    break;
                case ToolController.Tool.REPLACER:
                    if (ReplacerManager.settings.command == ISelectionBrushTool.Command.SELECT_BRUSH_PREFABS)
                        UpdateOctree();
                    BrushstrokeManager.ClearReplacerDictionary();
                    break;
                case ToolController.Tool.CIRCLE_SELECT:
                    if (CircleSelectManager.settings.command == ISelectionBrushTool.Command.SELECT_BRUSH_PREFABS)
                        UpdateOctree();
                    break;
                case ToolController.Tool.FLOOR:
                    UpdateFloorSettingsOnBrushChanged();
                    break;
                case ToolController.Tool.WALL:
                    UpdateWallSettingsOnBrushChanged();
                    break;
                case ToolController.Tool.BLOCK:
#if PWB_BLOCK
                    UpdateBlockSettingsOnBrushChanged();
#endif
                    break;
            }
        }

        private static bool _mousePressed;
        public static bool mousePressed => _mousePressed;
        public static void HandleMouseEvents()
        {
            if (Event.current.type == EventType.MouseDown) _mousePressed = true;
            else if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseLeaveWindow)
                _mousePressed = false;
        }

        public static void DuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            _sceneViewCamera = sceneView.camera;
            HandleMouseEvents();
            if (updateStroke) UnityEditor.SceneView.RepaintAll();
            if (sceneView.in2DMode)
            {
                GridManager.settings.gridOnZ = true;
                PWBToolbar.RepaintWindow();
            }
            if (repaint)
            {
                if (tool == ToolController.Tool.SHAPE) BrushstrokeManager.UpdateShapeBrushstroke();
                sceneView.Repaint();
                repaint = false;
            }
            GizmosInput();
            if (_offsetPicking)
            {
                OffsetPicking(sceneView.camera);
                var labelTexts = new string[] { $"Offset: {_offsetPickingValue.ToString("F5")}" };
                InfoText.Draw(sceneView, labelTexts.ToArray());
                if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
                {
                    _offsetPickingBrush.SetLocalPositionOffset(_offsetPickingValue, _offsetPickingAxis);
                    BrushProperties.RepaintWindow();
                    _offsetPicking = false;
                }
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                    _offsetPicking = false;
                sceneView.Repaint();
            }
            PaletteInput(sceneView);

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape
                && (tool == ToolController.Tool.PIN || tool == ToolController.Tool.BRUSH
                || tool == ToolController.Tool.GRAVITY || tool == ToolController.Tool.ERASER
                || tool == ToolController.Tool.REPLACER || tool == ToolController.Tool.CIRCLE_SELECT
                || tool == ToolController.Tool.FLOOR || tool == ToolController.Tool.WALL
                || tool == ToolController.Tool.BLOCK))
                ToolController.DeselectTool();
            var repaintScene = _wasPickingBrushes == PaletteManager.pickingBrushes;
            _wasPickingBrushes = PaletteManager.pickingBrushes;
            if (PaletteManager.pickingBrushes)
            {
                UnityEditor.HandleUtility.AddDefaultControl(_controlId);
                if (repaintScene) UnityEditor.SceneView.RepaintAll();
                if (Event.current.button == 0 && Event.current.type == EventType.MouseDown) Event.current.Use();
                return;
            }
            if (ToolController.current != ToolController.Tool.NONE)
            {
                if (PWBSettings.shortcuts.editModeToggle.Check())
                {
                    switch (tool)
                    {
                        case ToolController.Tool.LINE:
                        case ToolController.Tool.SHAPE:
                        case ToolController.Tool.TILING:
                            ToolController.editMode = !ToolController.editMode;
                            _persistentItemWasEdited = false;
                            ToolProperties.RepainWindow();
                            break;
                        default: break;
                    }
                }
                if (PaletteManager.selectedBrushIdx == -1 && (tool == ToolController.Tool.PIN
                    || tool == ToolController.Tool.BRUSH || tool == ToolController.Tool.GRAVITY
                    || ((tool == ToolController.Tool.LINE || tool == ToolController.Tool.SHAPE
                    || tool == ToolController.Tool.TILING)
                    && !ToolController.editMode)))
                {
                    if (tool == ToolController.Tool.LINE && _lineData != null && _lineData.state != ToolController.ToolState.NONE)
                        ResetLineState();
                    else if (tool == ToolController.Tool.SHAPE
                        && _shapeData != null && _shapeData.state != ToolController.ToolState.NONE)
                        ResetShapeState();
                    else if (tool == ToolController.Tool.TILING
                        && _tilingData != null && _tilingData.state != ToolController.ToolState.NONE)
                        ResetTilingState();
                }

                if (Event.current.type == EventType.MouseEnterWindow) _pinned = false;

                if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag)
                {
                    sceneView.Focus();
                }
                else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.V)
                    _snapToVertex = true;
                else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.V)
                    _snapToVertex = false;
                if (tool == ToolController.Tool.BRUSH || tool == ToolController.Tool.GRAVITY
                    || tool == ToolController.Tool.ERASER || tool == ToolController.Tool.REPLACER
                    || tool == ToolController.Tool.CIRCLE_SELECT)
                {
                    var settings = ToolController.GetSettingsFromTool(tool);
                    BrushRadiusShortcuts(settings as CircleToolBase);
                }

                switch (tool)
                {
                    case ToolController.Tool.PIN:
                        PinDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.BRUSH:
                        BrushDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.GRAVITY:
                        GravityToolDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.LINE:
                        LineDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.SHAPE:
                        ShapeDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.TILING:
                        TilingDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.ERASER:
                        EraserDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.REPLACER:
                        ReplacerDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.SELECTION:
                        SelectionDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.CIRCLE_SELECT:
                        CircleSelectDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.EXTRUDE:
                        ExtrudeDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.MIRROR:
                        MirrorDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.FLOOR:
                        FloorToolDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.WALL:
                        WallToolDuringSceneGUI(sceneView);
                        break;
                    case ToolController.Tool.BLOCK:
#if PWB_BLOCK
                        BlockToolDuringSceneGUI(sceneView);
#endif
                        break;
                }

                if ((tool != ToolController.Tool.EXTRUDE && tool != ToolController.Tool.SELECTION
                    && tool != ToolController.Tool.MIRROR) && Event.current.type == EventType.Layout
                    && !ToolController.editMode)
                {
                    UnityEditor.Tools.current = UnityEditor.Tool.None;
                    UnityEditor.HandleUtility.AddDefaultControl(_controlId);
                }
            }
            GridDuringSceneGui(sceneView);
            sceneView.autoRepaintOnSceneChange = true;
        }
        #endregion

        #region UNITY TOOL
        public static void SaveUnityCurrentTool() => _unityCurrentTool = UnityEditor.Tools.current;
        public static bool _wasPickingBrushes = false;
        public static void ResetUnityCurrentTool()
        {
            if (_unityCurrentTool != UnityEditor.Tool.None)
                UnityEditor.Tools.current = _unityCurrentTool;
        }

        #endregion
    }
}