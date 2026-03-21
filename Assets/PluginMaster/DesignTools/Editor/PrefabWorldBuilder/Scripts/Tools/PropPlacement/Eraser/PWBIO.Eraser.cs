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
using UnityEngine;
namespace PluginMaster
{
    public static partial class PWBIO
    {
        private static float _lastHitDistance = 20f;

        private static System.Collections.Generic.HashSet<GameObject> _toErase
            = new System.Collections.Generic.HashSet<GameObject>();

        private static readonly System.Collections.Generic.Dictionary<GameObject, MeshFilter[]> _eraseMeshFilterCache
            = new System.Collections.Generic.Dictionary<GameObject, MeshFilter[]>();

        private static Vector2 _lastEraserMousePos;

        private static void EraserMouseEvents()
        {
            if (Event.current.button == 0 && !Event.current.alt
                && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
            {
                Erase();
                Event.current.Use();
            }
            if (Event.current.button == 1)
            {
                if (Event.current.type == EventType.MouseDown && (Event.current.control || Event.current.shift))
                {
                    _pinned = true;
                    _pinMouse = Event.current.mousePosition;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp) _pinned = false;
            }
        }

        private static void Erase()
        {
            void EraseObject(GameObject obj)
            {
                if (obj == null) return;
                if (EraserManager.settings.outermostPrefabFilter)
                {
                    var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                    if (root != null) obj = root;
                }
                else
                {
                    var parent = obj.transform.parent.gameObject;
                    if (parent != null)
                    {
                        GameObject outermost = null;
                        do
                        {
                            outermost = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
                            if (outermost == null) break;
                            if (outermost == obj) break;
                            UnityEditor.PrefabUtility.UnpackPrefabInstance(outermost,
                                UnityEditor.PrefabUnpackMode.OutermostRoot, UnityEditor.InteractionMode.UserAction);
                        } while (outermost != parent);
                    }
                }
#if UNITY_6000_3_OR_NEWER
                PWBCore.DestroyTempCollider(obj.GetEntityId());
#else
                PWBCore.DestroyTempCollider(obj.GetInstanceID());
#endif
                _eraseMeshFilterCache.Remove(obj);
                UnityEditor.Undo.DestroyObjectImmediate(obj);
            }
            foreach (var obj in _toErase) EraseObject(obj);
            _toErase.Clear();
            _eraseMeshFilterCache.Clear();
        }

        private static void EraserDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            EraserMouseEvents();
            var mousePos = Event.current.mousePosition;
            if (_pinned) mousePos = _pinMouse;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos);

            var center = mouseRay.GetPoint(_lastHitDistance);
            if (PWBToolRaycast(mouseRay, out RaycastHit mouseHit, out GameObject collider,
                float.MaxValue, -1, paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true, ignoreSceneColliders: true,
                createTempColliders: true))
            {
                _lastHitDistance = mouseHit.distance;
                center = mouseHit.point;
            }
            DrawCircleTool(center, sceneView.camera, new Color(1f, 0.0f, 0, 1f), EraserManager.settings.radius);

            if (_lastEraserMousePos != mousePos)
            {
                _lastEraserMousePos = mousePos;
                _eraseMeshFilterCache.Clear();
                GetCircleToolTargets(mouseRay, sceneView.camera, EraserManager.settings,
                    EraserManager.settings.radius, _toErase);
            }

            DrawObjectsToErase(sceneView.camera);
        }

        private static void DrawObjectsToErase(Camera camera)
        {
            if (Event.current.type != EventType.Repaint) return;
            foreach (var obj in _toErase)
            {
                if (obj == null) continue;

                if (!_eraseMeshFilterCache.TryGetValue(obj, out var filters))
                {
                    filters = obj.GetComponentsInChildren<MeshFilter>();
                    _eraseMeshFilterCache[obj] = filters;
                }

                foreach (var filter in filters)
                {
                    var mesh = filter.sharedMesh;
                    if (mesh == null) continue;
                    for (int subMeshIdx = 0; subMeshIdx < mesh.subMeshCount; ++subMeshIdx)
                        Graphics.DrawMesh(mesh, filter.transform.localToWorldMatrix,
                            transparentRedMaterial, 0, camera, subMeshIdx);
                }
            }
        }
    }
}
