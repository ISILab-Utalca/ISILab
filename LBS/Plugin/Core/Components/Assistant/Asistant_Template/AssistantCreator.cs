using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace ISILab
{
    public static class AssistantCreator
    {
        [MenuItem("Assets/Create/ISILab/LBS/Assistant")]
        public static void Create()
        {
            var action = ScriptableObject.CreateInstance<CreateAssistantWithEditorAction>();

            try
            {
                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                    0,
                    action,
                    "NewAssistant.cs",
                    null,
                    "Assets/ISILab/LBS/Plugin/Core/Components/Assistant/Asistant_Template/AssistantTemplate.cs.txt");
            }
            catch
            {
                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                    0,
                    action,
                    "NewAssistant.cs",
                    null,
                    "Packages/Level Building Sidekick/LBS/Plugin/Core/Components/Assistant/Asistant_Template/AssistantTemplate.cs.txt");
            }
        }
    }

    public class CreateAssistantWithEditorAction : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            // Create the runtime script
            string template = File.ReadAllText(resourceFile);
            string className = Path.GetFileNameWithoutExtension(pathName);

            template = template.Replace("#SCRIPTNAME#", className);

            File.WriteAllText(pathName, template);

            // Create Editor folder next to script
            string directory = Path.GetDirectoryName(pathName);
            string editorDirectory = Path.Combine(directory, "Editor");

            if (!Directory.Exists(editorDirectory))
                Directory.CreateDirectory(editorDirectory);

            string editorPath =
                Path.Combine(editorDirectory, $"{className}Editor.cs");

            string editorTemplate = File.ReadAllText(
                "Assets/ISILab/LBS/Plugin/Core/Components/Assistant/Asistant_Template/AssistantTemplateEditor.cs.txt");

            editorTemplate = editorTemplate.Replace(
                "#TARGETCLASS#",
                className);

            File.WriteAllText(editorPath, editorTemplate);

            AssetDatabase.Refresh();
        }
    }
}
