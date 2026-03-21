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
#if UNITY_2021_2_OR_NEWER
using UnityEngine;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace PluginMaster
{
    public class PWBToolbarButton : UnityEditor.Toolbars.EditorToolbarButton
    {
        protected string _iconName = string.Empty;
        protected async void DoLoadIcon()
        {
            await System.Threading.Tasks.Task.Delay(1000);
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class PropertiesButton : PWBToolbarButton
    {
        public const string ID = "PWB/PropertiesButton";
        public PropertiesButton()
        {
            _iconName = "ToolProperties";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "Tool Properties";
            clicked += ToolProperties.ShowWindow;
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class BrushPropertiesButton : PWBToolbarButton
    {
        public const string ID = "PWB/BrushPropertiesButton";
        public BrushPropertiesButton()
        {
            _iconName = "BrushProperties";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "Brush Properties";
            clicked += BrushProperties.ShowWindow;
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class HelpButton : PWBToolbarButton
    {
        public const string ID = "PWB/HelpButton";
        public HelpButton()
        {
            _iconName = "Help";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "Documentation";
            clicked += PWBCore.OpenDocFile;
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class GridSettingsButton : PWBToolbarButton
    {
        public const string ID = "PWB/GridSettingsButton";
        public GridSettingsButton()
        {
            _iconName = "SnapSettings";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "Grid & Snapping Settings";
            clicked += SnapSettingsWindow.ShowWindow;
        }
    }

    [UnityEditor.Toolbars.EditorToolbarElement(ID, typeof(UnityEditor.SceneView))]
    public class PreferencesButton : PWBToolbarButton
    {
        public const string ID = "PWB/PreferencesButton";
        public PreferencesButton()
        {
            _iconName = "Preferences";
            icon = Resources.Load<Texture2D>(ToggleManager.iconPath + _iconName);
            if (icon == null) DoLoadIcon();
            tooltip = "PWB Preferences";
            clicked += PWBPreferences.ShowWindow;
        }
    }

    [UnityEditor.Overlays.Overlay(typeof(UnityEditor.SceneView), "PWB/Settings", true)]
    public class SettingsAndDocsToolbarOverlay : UnityEditor.Overlays.ToolbarOverlay
    {
        private static bool _isDisplayed = false;
        SettingsAndDocsToolbarOverlay()
            : base(PropertiesButton.ID, BrushPropertiesButton.ID, GridSettingsButton.ID, PreferencesButton.ID, HelpButton.ID)
        {
            displayedChanged += OndisplayedChanged;
#if UNITY_2022_2_OR_NEWER
            collapsedIcon = Resources.Load<Texture2D>(ToggleManager.iconPath + "Preferences");
#endif
        }

        private void OndisplayedChanged(bool value)
        {
            _isDisplayed = value;
            ToolbarOverlayManager.OnToolbarDisplayedChanged();
        }

        public static bool IsDisplayed => _isDisplayed;
    }
}
#endif