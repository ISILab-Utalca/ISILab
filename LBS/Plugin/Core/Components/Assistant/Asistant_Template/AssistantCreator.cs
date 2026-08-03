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

            UnityEditor.PackageManager.PackageInfo package = 
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AssistantCreator).Assembly);


            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                action,
                "NewAssistant.cs",
                null,
                package.assetPath);
        }
    }

    public class CreateAssistantWithEditorAction : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string assetPath)
        {
            // Create the runtime script
            string templatePath = Path.Combine(assetPath,
                "LBS/Plugin/Core/Components/Assistant/Assistant_Template/AssistantTemplate.cs.txt");
            string template = File.ReadAllText(templatePath);
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

            templatePath = Path.Combine(assetPath,
                "LBS/Plugin/Core/Components/Assistant/Assistant_Template/AssistantTemplateEditor.cs.txt");
            string editorTemplate = File.ReadAllText(templatePath);

            editorTemplate = editorTemplate.Replace(
                "#TARGETCLASS#",
                className);

            File.WriteAllText(editorPath, editorTemplate);

            AssetDatabase.Refresh();
        }
    }
}
