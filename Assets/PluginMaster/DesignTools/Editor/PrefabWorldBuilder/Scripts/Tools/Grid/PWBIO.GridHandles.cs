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
        private static void GridHandles()
        {
            if (!GridManager.settings.lockedGrid) return;
            var originOffset = GridManager.settings.origin;
            var rotation = GridManager.settings.rotation;
            var snapSize = GridManager.settings.step;
            UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            var handleSize = UnityEditor.HandleUtility.GetHandleSize(originOffset);

            void DrawSnapGizmos(AxesUtils.Axis forwardAxis, AxesUtils.Axis upwardAxis)
            {
                var fw = rotation * AxesUtils.GetVector(1, forwardAxis);
                var uw = rotation * AxesUtils.GetVector(1, upwardAxis);
                var coneSize = handleSize * 0.15f;
                var stepSize = GridManager.settings.radialGridEnabled ? GridManager.settings.radialStep
                    : AxesUtils.GetAxisValue(snapSize, forwardAxis);
                var conePosFw = originOffset + fw * (handleSize * 1.6f);
                var originScreenPos = _sceneViewCamera.WorldToScreenPoint(GridManager.settings.origin);
                var fwScreenPos = _sceneViewCamera.WorldToScreenPoint(conePosFw);
                var alpha = Mathf.Clamp01((fwScreenPos - originScreenPos).magnitude / 90 - 0.5f);

                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                float distFromMouse = UnityEditor.HandleUtility.DistanceToCircle(conePosFw, coneSize / 2);
                UnityEditor.HandleUtility.AddControl(controlId, distFromMouse);
                bool mouseOver = UnityEditor.HandleUtility.nearestControl == controlId;

                UnityEditor.Handles.color = new Color(1f, 1f, mouseOver ? 1 : 0, alpha);
                UnityEditor.Handles.ConeHandleCap(controlId, conePosFw,
                    Quaternion.LookRotation(fw, uw), coneSize, EventType.Repaint);
                if (Event.current.button == 0 && Event.current.type == EventType.MouseDown && mouseOver)
                    GridManager.settings.origin += fw * stepSize;

                var conePosBw = originOffset + fw * (handleSize * 1.3f);
                controlId = GUIUtility.GetControlID(FocusType.Passive);
                distFromMouse = UnityEditor.HandleUtility.DistanceToCircle(conePosBw, coneSize / 2);
                UnityEditor.HandleUtility.AddControl(controlId, distFromMouse);
                mouseOver = UnityEditor.HandleUtility.nearestControl == controlId;

                UnityEditor.Handles.color = new Color(1f, 1f, mouseOver ? 1 : 0, alpha);
                UnityEditor.Handles.ConeHandleCap(controlId, conePosBw,
                    Quaternion.LookRotation(-fw, -uw), coneSize, EventType.Repaint);
                if (Event.current.button == 0 && Event.current.type == EventType.MouseDown && mouseOver)
                    GridManager.settings.origin -= fw * stepSize;
            }
            if (GridManager.settings.showPositionHandle)
            {
                GridManager.settings.origin = UnityEditor.Handles.PositionHandle(originOffset, rotation);
                UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                UnityEditor.Handles.color = Color.yellow;
                UnityEditor.Handles.SphereHandleCap(0, originOffset, rotation,
                    UnityEditor.HandleUtility.GetHandleSize(originOffset) * 0.2f, EventType.Repaint);
                UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                DrawSnapGizmos(AxesUtils.Axis.X, AxesUtils.Axis.Y);
                DrawSnapGizmos(AxesUtils.Axis.Y, AxesUtils.Axis.Z);
                DrawSnapGizmos(AxesUtils.Axis.Z, AxesUtils.Axis.X);
            }
            else if (GridManager.settings.showRotationHandle)
                GridManager.settings.rotation = UnityEditor.Handles.RotationHandle(rotation, originOffset);
            else if (GridManager.settings.showScaleHandle)
            {
                if (GridManager.settings.radialGridEnabled)
                {
                    var step0 = Vector3.one * GridManager.settings.radialStep;
                    var step = UnityEditor.Handles.ScaleHandle(step0, originOffset,
                        rotation, handleSize);
                    if (step0 != step)
                    {
                        if (step0.x != step.x) GridManager.settings.radialStep = step.x;
                        else if (step0.y != step.y) GridManager.settings.radialStep = step.y;
                        else GridManager.settings.radialStep = step.z;
                    }
                }
                else
                {
                    GridManager.settings.step = UnityEditor.Handles.ScaleHandle(GridManager.settings.step,
                    originOffset, rotation, handleSize);
                }
            }
            if (GridManager.settings.origin != originOffset
                || GridManager.settings.rotation != rotation
                || GridManager.settings.step != snapSize)
                SnapSettingsWindow.RepaintWindow();
        }
    }
}
