/*
Copyright(c) Omar Duarte
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
using UnityEngine;

namespace PluginMaster
{
    public static partial class PWBIO
    {
        #region HANDLERS
        private static void FloorInitializeOnLoad()
        {
            FloorManager.settings.OnDataChanged += OnFloorSettingsChanged;
            BrushSettings.OnBrushSettingsChanged += UpdateFloorSettingsOnBrushChanged;
            GridManager.settings.OnGridOriginChange += OnFloorGridOriginChange;
        }

        private static void SetSnapStepToFloorCellSize()
        {
            GridManager.settings.step = FloorManager.settings.moduleSize + FloorManager.settings.spacing;
            UnityEditor.SceneView.RepaintAll();
        }

        private static void OnFloorSettingsChanged()
        {
            repaint = true;
            BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false);
            SetSnapStepToFloorCellSize();
        }

        public static void UpdateFloorSettingsOnBrushChanged()
        {
            if (ToolController.current != ToolController.Tool.FLOOR) return;
            FloorManager.quarterTurns = 0;
            FloorManager.settings.UpdateCellSize();
            SetSnapStepToFloorCellSize();
            FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
        }

        public static void OnFloorGridOriginChange()
        {
            if (ToolController.current != ToolController.Tool.FLOOR) return;
            repaint = true;
            BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false);
            SetSnapStepToFloorCellSize();
        }
        #endregion
        public static void OnFloorEnabled()
        {
            UpdateOctree();
            GridManager.settings.radialGridEnabled = false;
            GridManager.settings.gridOnY = true;
            GridManager.settings.visibleGrid = true;
            GridManager.settings.lockedGrid = true;
            GridManager.settings.snappingOnX = true;
            GridManager.settings.snappingOnZ = true;
            GridManager.settings.snappingEnabled = true;
            UpdateFloorSettingsOnBrushChanged();
            GridManager.settings.DataChanged(repaint: true);
            FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
            FloorManager.quarterTurns = 0;
        }

        private static void FloorToolDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (PaletteManager.selectedBrush == null) return;
            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);
            var mousePos3D = Vector3.zero;
            var localMousePos3D = Vector3.zero;
            if (GridRaycast(mouseRay, out RaycastHit gridHit))
                mousePos3D = SnapFloorTilePosition(gridHit.point, out localMousePos3D);
            else return;
            if (FloorInput(sceneView.camera, mousePos3D)) return;

            switch (FloorManager.state)
            {
                case FloorManager.ToolState.FIRST_CORNER:
                    PreviewFloorSingleTile(sceneView.camera, mousePos3D);
                    break;
                case FloorManager.ToolState.SECOND_CORNER:
                    PreviewFloorRectangle(sceneView.camera);
                    break;
            }
            FloorInfoText(sceneView, localMousePos3D);
        }
        private static void FloorInfoText(UnityEditor.SceneView sceneView, Vector3 localMousePos3D)
        {
            if (!PWBCore.staticData.showInfoText) return;
            var localX = Mathf.RoundToInt(localMousePos3D.x / GridManager.settings.step.x);
            if (localX >= 0) ++localX;
            var localZ = Mathf.RoundToInt(localMousePos3D.z / GridManager.settings.step.z);
            if (localZ >= 0) ++localZ;
            var labelTexts = new string[] { $"Position: (X: {localX}, Z: {localZ})",
                    $"Size: (X: {BrushstrokeManager.cellsCountX}, Z: {BrushstrokeManager.cellsCountZ})"};
            InfoText.Draw(sceneView, labelTexts);

        }
        private static Vector3 _floorSecondCorner = Vector3.zero;
        private static bool FloorInput(Camera camera, Vector3 mousePos3D)
        {

            if ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown))
            {
                if (Event.current.control && !Event.current.alt && !Event.current.shift) _modularDeleteMode = true;

                else if (_modularDeleteMode && (!Event.current.control || Event.current.alt || Event.current.shift))
                {
                    _modularDeleteMode = false;
                    FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
                    BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: true);
                    return true;
                }
            }
            if (PWBSettings.shortcuts.floorRotate90YCW.Check())
            {
                ++FloorManager.quarterTurns;
                if (FloorManager.quarterTurns >= 4) FloorManager.quarterTurns = 0;
                FloorManager.settings.UpdateCellSize();
                SetSnapStepToFloorCellSize();
                FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
                BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false);
                return true;
            }
            if (Event.current.button == 0)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    FloorManager.state = FloorManager.ToolState.SECOND_CORNER;
                    FloorManager.secondCorner = FloorManager.firstCorner = mousePos3D;
                    BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false, _modularDeleteMode);
                    return true;
                }
                if (FloorManager.state == FloorManager.ToolState.SECOND_CORNER)
                {
                    if (Event.current.type == EventType.MouseDrag)
                    {
                        FloorManager.secondCorner = mousePos3D;
                        if (_floorSecondCorner != FloorManager.secondCorner)
                            BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: true, _modularDeleteMode);
                    }
                    if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseMove)
                    {
                        FloorManager.secondCorner = mousePos3D;
                        var paintStrokeCount = _paintStroke.Count;
                        if (_modularDeleteMode)
                        {
                            BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false, _modularDeleteMode);
                            DeleteFloor();
                        }
                        else Paint(FloorManager.settings);
                        FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
                        if (paintStrokeCount == 1) BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: true);
                        return true;
                    }
                }
                _floorSecondCorner = FloorManager.secondCorner;
            }
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                FloorManager.state = FloorManager.ToolState.FIRST_CORNER;
                BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false);
                return true;
            }
            return false;
        }
        private static void PreviewFloorSingleTile(Camera camera, Vector3 mousePos3D)
        {
            BrushstrokeItem[] brushstroke = BrushstrokeManager.brushstroke;
            if (brushstroke.Length == 0) return;
            var strokeItem = brushstroke[0].Clone();
            if (strokeItem.settings == null)
            {
                BrushstrokeManager.UpdateFloorBrushstroke(setNextIdx: false);
                return;
            }
            var prefab = strokeItem.settings.prefab;
            if (prefab == null) return;
            var toolSettings = FloorManager.settings;
            var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
            var previewRotation = itemRotation;
            previewRotation *= Quaternion.Inverse(prefab.transform.rotation);
            var cellCenter = mousePos3D;
            BrushSettings brush = strokeItem.settings;
            if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
            cellCenter += itemRotation * brush.localPositionOffset;
            if (_modularDeleteMode)
            {
                if (toolSettings.subtractBrushOffset)
                {
                    var r = GridManager.settings.rotation;
                    if (FloorManager.quarterTurns > 0)
                        r *= Quaternion.AngleAxis(FloorManager.quarterTurns * 90, toolSettings.upwardAxis);
                    cellCenter -= r * (brush.localPositionOffset * 0.5f);
                }
                var TRS = Matrix4x4.TRS(cellCenter, GridManager.settings.rotation, toolSettings.moduleSize);
                Graphics.DrawMesh(cubeMesh, TRS, transparentRedMaterial2, 0, camera);
                return;
            }

            var halfStep = Mathf.Min(GridManager.settings.step.x, GridManager.settings.step.z) * 0.4999;
            var halfCellSize = toolSettings.moduleSize / 2;
            var nearbyObjects = new System.Collections.Generic.List<GameObject>();
            boundsOctree.GetColliding(cellCenter, halfCellSize, GridManager.settings.rotation,
                itemRotation, nearbyObjects);
            if (nearbyObjects.Count > 0)
            {
                bool checkNextItem = false;
                foreach (var obj in nearbyObjects)
                {
                    if (obj == null) continue;
                    if (!obj.activeInHierarchy) continue;
                    var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                    var centerDistance = (objCenter - cellCenter).magnitude;
                    if (centerDistance > halfStep) continue;
                    if (PaletteManager.selectedPalette.ContainsSceneObject(obj))
                    {
                        checkNextItem = true;
                        break;
                    }
                }
                if (checkNextItem) return;
            }

            var scaleMult = strokeItem.scaleMultiplier;
            var centerToPivot = GetCenterToPivot(prefab, scaleMult, itemRotation);
            var itemPosition = cellCenter + centerToPivot;
            var translateMatrix = Matrix4x4.Translate(-prefab.transform.position);
            var rootToWorld = Matrix4x4.TRS(itemPosition, previewRotation, scaleMult) * translateMatrix;
            var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;

            PreviewBrushItem(prefab, rootToWorld, layer, camera,
                redMaterial: false, reverseTriangles: false, flipX: false, flipY: false);
            var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
            Transform parentTransform = toolSettings.parent;
            var paintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition,
                itemRotation * prefab.transform.rotation, itemScale, layer, parentTransform,
                surface: null, flipX: false, flipY: false);
            _paintStroke.Clear();
            _paintStroke.Add(paintItem);
        }

        private static void PreviewFloorRectangle(Camera camera)
        {
            BrushstrokeItem[] brushstroke = null;
            if (PreviewIfBrushtrokestaysTheSame(out brushstroke, camera, forceUpdate: _paintStroke.Count == 0))
                if (!_modularDeleteMode) return;
            if (brushstroke.Length == 0) return;
            _paintStroke.Clear();
            var toolSettings = FloorManager.settings;
            var halfCellSize = toolSettings.moduleSize / 2;
            var halfStep = Mathf.Min(GridManager.settings.step.x, GridManager.settings.step.z) * 0.4999;
            if (_modularDeleteMode) _floorDeleteStroke.Clear();
            for (int i = 0; i < brushstroke.Length; ++i)
            {
                var strokeItem = brushstroke[i];
                if (strokeItem.settings == null) return;
                var prefab = strokeItem.settings.prefab;
                if (prefab == null) return;
                var scaleMult = strokeItem.scaleMultiplier;
                var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
                var quarterTurns = FloorManager.quarterTurns;
                if (FloorManager.settings.swapXZ) ++quarterTurns;
                if (quarterTurns > 0)
                    itemRotation = itemRotation
                        * Quaternion.AngleAxis(90 * quarterTurns, toolSettings.upwardAxis);
                var cellCenter = strokeItem.tangentPosition;

                if (_modularDeleteMode)
                {
                    if (toolSettings.subtractBrushOffset)
                    {
                        BrushSettings brush = strokeItem.settings;
                        if (toolSettings.overwriteBrushProperties) brush = toolSettings.brushSettings;
                        var r = GridManager.settings.rotation;
                        if (FloorManager.quarterTurns > 0)
                            r *= Quaternion.AngleAxis(FloorManager.quarterTurns * 90, toolSettings.upwardAxis);
                        cellCenter -= r * (brush.localPositionOffset * 0.5f);
                    }
                    var TRS = Matrix4x4.TRS(cellCenter, GridManager.settings.rotation, FloorManager.settings.moduleSize);
                    Graphics.DrawMesh(cubeMesh, TRS, transparentRedMaterial2, layer: 0, camera);
                    _floorDeleteStroke.Add(new Pose(cellCenter, Quaternion.Euler(strokeItem.additionalAngle)));
                    continue;
                }

                var centerToPivot = GetCenterToPivot(prefab, scaleMult, itemRotation);
                var itemPosition = cellCenter + centerToPivot;

                var nearbyObjects = new System.Collections.Generic.List<GameObject>();
                boundsOctree.GetColliding(cellCenter, halfCellSize, GridManager.settings.rotation,
                    itemRotation, nearbyObjects);
                if (nearbyObjects.Count > 0)
                {
                    bool checkNextItem = false;
                    foreach (var obj in nearbyObjects)
                    {
                        if (obj == null) continue;
                        if (!obj.activeInHierarchy) continue;
                        var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                        var centerDistance = (objCenter - cellCenter).magnitude;
                        if (centerDistance > halfStep) continue;
                        if (PaletteManager.selectedPalette.ContainsSceneObject(obj))
                        {
                            checkNextItem = true;
                            break;
                        }
                    }
                    if (checkNextItem) continue;
                }

                var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;

                var previewRotation = Quaternion.Inverse(prefab.transform.rotation) * itemRotation;
                var previewRootToWorld = Matrix4x4.TRS(itemPosition + previewRotation * -prefab.transform.position,
                    previewRotation, scaleMult);
                PreviewBrushItem(prefab, previewRootToWorld, layer, camera,
                    redMaterial: false, reverseTriangles: false, flipX: false, flipY: false);
                _previewData.Add(new PreviewData(prefab, previewRootToWorld, layer, flipX: false, flipY: false));
                var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
                Transform parentTransform = toolSettings.parent;
                var paintItem = new PaintStrokeItem(prefab, strokeItem.settings.guid, itemPosition, itemRotation,
                    itemScale, layer, parentTransform, surface: null, flipX: false, flipY: false);
                _paintStroke.Add(paintItem);
            }
        }

        private static System.Collections.Generic.HashSet<Pose> _floorDeleteStroke
            = new System.Collections.Generic.HashSet<Pose>();
        private static void DeleteFloor()
        {
            if (_floorDeleteStroke.Count == 0) return;
            var toolSettings = FloorManager.settings;
            var toBeDeleted = new System.Collections.Generic.HashSet<GameObject>();
            var halfCellSize = toolSettings.moduleSize / 2;
            foreach (var cellPose in _floorDeleteStroke)
            {
                var nearbyObjects = new System.Collections.Generic.List<GameObject>();
                boundsOctree.GetColliding(cellPose.position, halfCellSize,
                    GridManager.settings.rotation, cellPose.rotation, nearbyObjects);
                if (nearbyObjects.Count == 0) continue;

                foreach (var obj in nearbyObjects)
                {
                    if (obj == null) continue;
                    if (!obj.activeInHierarchy) continue;
                    var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                    var centerDistance = (objCenter - cellPose.position).magnitude;
                    var halfStep = Mathf.Min(GridManager.settings.step.x, GridManager.settings.step.z) * 0.4999;
                    if (centerDistance > halfStep) continue;
                    if (PaletteManager.selectedPalette.ContainsSceneObject(obj)) toBeDeleted.Add(obj);
                }
            }
            void EraseObject(GameObject obj)
            {
                if (obj == null) return;
                var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                if (root != null) obj = root;
#if UNITY_6000_3_OR_NEWER
                PWBCore.DestroyTempCollider(obj.GetEntityId());
#else
                PWBCore.DestroyTempCollider(obj.GetInstanceID());
#endif
                UnityEditor.Undo.DestroyObjectImmediate(obj);
            }
            foreach (var obj in toBeDeleted) EraseObject(obj);
        }
    }
}