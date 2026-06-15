using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.Internal;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.Core.AI.Optimization.EvolutionaryAlgorithm.Evaluators
{
    [InitializeOnLoad]
    public class EvaluatorWatcher
    {
        static FileSystemWatcher watcher;
        static EvaluatorsDatabase database;
        static bool databaseLoaded = false;
        static readonly Queue<string> pendingPaths = new Queue<string>();

        static EvaluatorWatcher()
        {
            string path = Path.GetFullPath(LBSSettings.Instance.paths.evaluatorsPath);

            watcher = new FileSystemWatcher(Path.GetDirectoryName(path));

            watcher.Changed += OnChanged;
            watcher.Filter = "*.cs";
            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = true;
        }

        static void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(e.FullPath)) return;
            if (e.FullPath.Contains("Editor")) return;
            Debug.Log("Modificado: " + e.FullPath);
            lock (pendingPaths)
            {
                pendingPaths.Enqueue(e.FullPath);
                EditorApplication.update -= ProcessPending;
                EditorApplication.update += ProcessPending;
            }
        }

        static void ProcessPending()
        {
            if (!databaseLoaded)
            {
                string[] guids = AssetDatabase.FindAssets("t:EvaluatorsDatabase");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    database = AssetDatabase.LoadAssetAtPath<EvaluatorsDatabase>(path);
                    if (database == null)
                        Debug.LogError("Failed to load Evaluators Database.");
                    else databaseLoaded = true;
                }
                return;
            }

            lock (pendingPaths)
            {
                while (pendingPaths.Count > 0)
                {
                    string path = pendingPaths.Dequeue();
                    string file = Path.GetFileNameWithoutExtension(path);
                    Debug.Log("Current file: " + file);
                    EvaluatorData ev = database.ReturnEvaluatorByName(file);

                    List<string> lines = new(File.ReadAllLines(path));
                    int start = lines.FindIndex(l => l.Contains("#region CHARACTERISTIC FIELDS"));

                    bool addedOrRemoved = ev.AddingOrRemovingParameter;
                    for(int i = 0; i < ev.ParamList.Count; i++)
                    {
                        int ind = 1 + start;
                        bool found = false;
                        while (ind < lines.Count && !lines[ind].Contains("#FIELDS_DECLARATION#"))
                        {
                            if (lines[ind].Contains(" " + ev.ParamList[i].name + ";"))
                            {
                                found = true; break;
                            }
                            ind++;
                        }
                        if (!found)
                        {
                            var e = ev.ParamList[i];
                            e.state = ParameterCreateState.Deleted;
                            ev.ParamList[i] = e;
                            ev.ParamList.RemoveAt(i--); // Tal vez esta es la unica linea necesaria?
                        }

                        if (ev.ParamList[i].state.Equals(ParameterCreateState.Defined))
                        {
                            var e = ev.ParamList[i];
                            e.state = ParameterCreateState.JustCreated;
                            ev.ParamList[i] = e;
                        }
                        else if (!addedOrRemoved && ev.ParamList[i].state.Equals(ParameterCreateState.JustCreated))
                        {
                            var e = ev.ParamList[i];
                            e.state = ParameterCreateState.PreviouslyCreated;
                            ev.ParamList[i] = e;
                        }
                        //else if (ev.ParamList[i].state.Equals(ParameterCreateState.Deleted))
                        //    ev.ParamList.RemoveAt(i--);
                    }
                    if (addedOrRemoved) ev.AddingOrRemovingParameter = false;
                }
                Debug.Log("Pending paths resolved.");

                if (pendingPaths.Count == 0)
                    EditorApplication.update -= ProcessPending;
            }
        }
    }
}
