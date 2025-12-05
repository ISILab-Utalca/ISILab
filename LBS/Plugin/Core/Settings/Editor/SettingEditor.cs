using ISILab.Commons.Utility.Editor;

namespace ISILab.LBS.Plugin.Core.Settings.Editor
{
    public class SettingsEditor
    {
        public static void SearchSettingsInstance()
        {
            LBSSettings.Instance = DirectoryTools.GetScriptable<LBSSettings>();
        }
    }
}