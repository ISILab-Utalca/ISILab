using UnityEditor;

namespace ISILab.LBS.Plugin.Core.Settings.Editor
{
    [InitializeOnLoad]
    public class Startup
    {
        static Startup()
        {

            var onStart = SessionState.GetBool("start", true);
            if (onStart)
            {
                EditorApplication.update += Start;
                SessionState.SetBool("start", false);
            }
        }

        private static void Start()
        {
            // TODO: open a window that opens at the beginning of the use of unity

            SettingsEditor.SearchSettingsInstance();
            EditorApplication.update -= Start;
        }
    }
}