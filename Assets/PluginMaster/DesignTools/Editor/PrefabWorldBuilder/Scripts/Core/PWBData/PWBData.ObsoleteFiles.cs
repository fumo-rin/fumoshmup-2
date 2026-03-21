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

namespace PluginMaster
{
    public partial class PWBData
    {
        public void DeleteObsoleteFiles()
        {
            const string obsoleteFilesDeletedSessionStateKey = "PWBObsoleteFilesDeleted";
            if (UnityEditor.SessionState.GetBool(obsoleteFilesDeletedSessionStateKey, false)) return;
            else UnityEditor.SessionState.SetBool(obsoleteFilesDeletedSessionStateKey, true);

            var rootDirFullPath = PWBCore.GetFullPath(_rootDirectory);
            var obsoleteDir = rootDirFullPath + "/Scripts";
            if (!System.IO.Directory.Exists(obsoleteDir)) return;
            var obsoleteDirNew = rootDirFullPath + "/Scripts/Obsolete";
            if (System.IO.Directory.Exists(obsoleteDirNew))
            {
                System.IO.Directory.Delete(obsoleteDirNew, true);
                var metaFilePath = obsoleteDirNew + ".meta";
                if (System.IO.File.Exists(metaFilePath))
                    System.IO.File.Delete(metaFilePath);
                PWBCore.refreshDatabase = true;
                return;
            }
            var files = new string[]
            {
               "Shortcuts.cs",
               "SnapManager.cs",
               "SnapSettingsWindow.cs",
               "ToolBase.cs",
               "ToolManager.cs"
            };
            var filesWereDeleted = false;
            foreach (var file in files)
            {
                var filePath = obsoleteDir + "/" + file;
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    var metaFilePath = filePath + ".meta";
                    if (System.IO.File.Exists(metaFilePath))
                        System.IO.File.Delete(metaFilePath);
                    filesWereDeleted = true;
                }
            }
            if (filesWereDeleted)
                PWBCore.refreshDatabase = true;
        }
    }
}