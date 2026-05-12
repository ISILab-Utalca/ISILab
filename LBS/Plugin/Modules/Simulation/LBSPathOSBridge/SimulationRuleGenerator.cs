using ISILab.Commons.Extensions;
using ISILab.LBS.Behaviours;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.MapTools.Generators;
using ISILab.LBS.Plugin.Modules.Simulation.PathOSPlus.OGVis.Scripts;
using LBS.Components;
using PathOS;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AI;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace ISILab.LBS.Plugin.Modules.Simulation.LBSPathOSBridge
{
    public class SimulationRuleGenerator : LBSGeneratorRule
    {
        #region FIELDS
        // PathOS+ Prefabs originales
        [System.NonSerialized]
        private GameObject agentPrefab;
        [System.NonSerialized]
        private GameObject managerPrefab;
        [System.NonSerialized]
        private GameObject worldCameraPrefab;
        [System.NonSerialized]
        private GameObject screenshotCameraPrefab;

        // Otros prefabs
        private GameObject elementPrefab;
        private GameObject wallPrefab;
        #endregion

        public SimulationRuleGenerator() : base() { }

        // For template construction
        public SimulationRuleGenerator(string IconGuid, string name, Color colorTint) : base() { }

        #region METHODS
        public override bool CheckViability(LBSLayer layer)
        {
            return true; // GABO TODO: Implementar? (Proyecto no la usa todavia)
        }

        public override object Clone()
        {
            return new SimulationRuleGenerator();
        }

        // GABO TODO: TERMINARR
        public override GeneratedGO Generate(LBSLayer layer, LBSGenerator3DSettings settings)
        {
#if UNITY_EDITOR
            PathOSStorage storage = PathOSStorage.Instance;

            // Get Prefabs
            agentPrefab = storage.agentPrefab.gameObject;
            managerPrefab = storage.managerPrefab.gameObject;
            worldCameraPrefab = storage.worldCameraPrefab.gameObject;
            screenshotCameraPrefab = storage.screenshotCameraPrefab.gameObject;
            elementPrefab = Resources.Load<GameObject>("Prefabs/ElementPrefab");
            wallPrefab = Resources.Load<GameObject>("Prefabs/WallPrefab");

            // Get PathOS window reference
            PathOSWindow window = 
                EditorWindow.GetWindow(typeof(PathOSWindow), false, "PathOS+", false) as PathOSWindow;

            // Setup
            GameObject parent = new GameObject("PathOS+");

            List<LBSGeneratedSimulation> allGeneratedComponents = new();
            List<(SimulationTile, LevelEntity)> generatedEntities = new();
            List<(SimulationTile, GameObject)> walls = new();
            Dictionary<string, int> objectNames = new();
            GameObject agentGO = null;

            // PathOS escentials
            PathOSManager manager = GenerateManager(parent.transform, window).GetComponent<PathOSManager>();

            GenerateScreenshotCamera(parent.transform, window);
            GenerateWorldCamera(parent.transform);

            // LBSFloor
            int notEmptyFloorCount = 0; // Only counts floor with items
            for (int i = 0; i < layer.FloorCount; i++)
            {
                // Floor variables
                SimulationModule simMod = layer.GetModule<SimulationModule>("", i);
                SimulationBehaviour simBehaviour = layer.GetBehaviour<SimulationBehaviour>();

                List<SimulationTile> tiles = simMod.GetTiles();
                if (tiles.Count < 1) continue;
                notEmptyFloorCount++;

                GameObject floorParent = new GameObject("Floor " + i);
                floorParent.transform.SetParent(parent.transform);
                floorParent.transform.localPosition = Vector3.up * i * settings.scale.y;

                // Objeto hijo, contenedor de los objetos etiquetados
                GameObject levelMarkupContainer = new GameObject("Level Markup");
                levelMarkupContainer.transform.SetParent(floorParent.transform);

                // 2do Objeto hijo, contenedor de los muros
                GameObject wallsContainer = new GameObject("Walls");
                wallsContainer.transform.SetParent(floorParent.transform);

                // 3er Objeto hijo, contenedor de escaleras
                GameObject stairContainer = new GameObject("Stairs");
                stairContainer.transform.SetParent(floorParent.transform);

                foreach (SimulationTile tile in tiles)
                {
                    var instance = GenerateSimulationTile(parent.transform, settings, tile, i);
                    
                    // Player settings
                    if(tile.Tag != null && tile.Tag.label == "Player" && agentGO is null)
                    {
                        agentGO = instance.gameObject;
                        window.SetAgentReference(agentGO.GetComponent<PathOSAgent>());
                        continue;
                    }
                    // Wall settings
                    else if(tile.Tag != null && tile.Tag.label == "Wall")
                    {
                        instance.transform.SetParent(wallsContainer.transform);
                        walls.Add((tile, instance.gameObject));
                    }
                    // Stair settings
                    else if(tile.Tag != null && tile.Tag == simBehaviour.upStairTag)
                    {
                        instance.transform.SetParent(stairContainer.transform);
                        instance.gameObject.name += " (Up)";
                        instance.transform.localPosition += (tile.Tag == simBehaviour.downStairTag) ? Vector3.up * settings.scale.y : Vector3.zero;
                        LevelEntity levelEntity = manager.AddLevelEntity(instance.gameObject, tile.EntityType);
                        instance.levelEntity = levelEntity;

                        var otherStairPosition = tile.StairRef.Positions[tile.StairRef.Positions.Count - 1];
                        var otherStairTile = new SimulationTile(simBehaviour, otherStairPosition.x, otherStairPosition.y, EntityType.ET_STAIR_DOWN, simBehaviour.downStairTag)
                        {
                            StairRef = tile.StairRef
                        };
                        var otherStair = GenerateSimulationTile(parent.transform, settings, otherStairTile, i+1);
                        otherStair.gameObject.name += " (Down)";
                        otherStair.transform.SetParent(stairContainer.transform);
                        LevelEntity otherLevelEntity = manager.AddLevelEntity(otherStair.gameObject, otherStairTile.EntityType);
                        otherStair.levelEntity = otherLevelEntity;
                        SetGeneratedName(otherStairTile, otherStair);

                        instance.levelEntity.OtherStairRef = otherLevelEntity;
                        otherStair.levelEntity.OtherStairRef = levelEntity;
                        instance.levelEntity.DirectionSign = 1;
                        otherStair.levelEntity.DirectionSign = -1;
                    }
                    // Entities settings
                    else
                    {
                        instance.transform.SetParent(levelMarkupContainer.transform);
                        //TODO: Cambiar instance por el elemento de Population que representa, cuando corresponda
                        LevelEntity levelEntity = manager.AddLevelEntity(instance.gameObject, tile.EntityType);
                        generatedEntities.Add((tile, levelEntity));
                        instance.levelEntity = levelEntity;
                    }

                    SetGeneratedName(tile, instance);

                    allGeneratedComponents.Add(instance);
                }

                void SetGeneratedName(SimulationTile tile, LBSGeneratedSimulation instance)
                {
                    string key = PathOS.UI.entityLabels[tile.EntityType];
                    instance.gameObject.name = key;
                    if (objectNames.ContainsKey(key))
                    {
                        objectNames[key]++;
                        instance.gameObject.name += " (" + objectNames[key] + ")";
                    }
                    else objectNames.Add(key, 0);
                }
            }

            // Error case: agentGameObject wasn't generated
            if(agentGO is null)
            {
                return new GeneratedGO(parent, 
                    new LBSLog("[SimulatorRuleGenerator]: The simulation layer needs a Player entity on the level to generate.",
                    LogType.Error, 3));
            }

            manager.floorCount = notEmptyFloorCount;
            manager.gameObject.GetComponent<OGLogManager>().floorCount = notEmptyFloorCount;
            var heatmapVisualizer = manager.gameObject.GetComponentInChildren<OGLogHeatmap>().gameObject;
            for(int i = 1; i < notEmptyFloorCount; i++)
            {
                GameObject.Instantiate(heatmapVisualizer, manager.gameObject.transform);
            }

            // Apply agent reference to all generated components
            PathOSAgent agentComp = agentGO.GetComponent<PathOSAgent>();
            agentComp.GetMemory().gridSampleSize = settings.scale;
            foreach (LBSGeneratedSimulation generated in allGeneratedComponents)
            {
                generated.agent = agentComp;
            }

            // Dynamic obstacles configuration
            // (currently doing nothing)
            foreach (var entityPair in generatedEntities)
            {
                if (!entityPair.Item1.IsDynamicObstacleTrigger) continue;

                foreach (var obstaclePair in entityPair.Item1.GetObstacles())
                {
                    if (obstaclePair.Item1.Tag.Label != "Wall")
                    {
                        var otherEntityPair = generatedEntities.Find(otherPair => otherPair.Item1 == obstaclePair.Item1);
                        //entityPair.Item2.dynamicObstacles.Add(new EntityObstaclePair(otherEntityPair.Item2.objectRef, obstaclePair.Item2));
                    }
                    else
                    {
                        var wallPair = walls.Find(wallPair => wallPair.Item1 == obstaclePair.Item1);
                        //entityPair.Item2.dynamicObstacles.Add(new EntityObstaclePair(wallPair.Item2, obstaclePair.Item2));
                    }
                }
            }

            // BAKING AUTOMATICO (asume que ya se encuentran instanciados los objetos de los otros modulos,
            // y que estos tienen colliders)
            GenerateNavMesh(walls).SetParent(parent);

            // Global position
            parent.transform.position += settings.position;

            return new GeneratedGO(parent, new LBSLog(0));
#else
                Debug.LogError("Attempting to use PathOSRuleGenerator class outside of Editor!"); return null;
#endif
        }

        private GameObject GenerateManager(Transform parent, PathOSWindow window)
        {
            GameObject mgo = PrefabUtility.InstantiatePrefab(managerPrefab, parent) as GameObject;
            window.SetManagerReference(mgo.GetComponent<PathOSManager>());
            return mgo;
        }

        private GameObject GenerateWorldCamera(Transform parent)
        {
            return PrefabUtility.InstantiatePrefab(worldCameraPrefab, parent) as GameObject;
        }

        private GameObject GenerateScreenshotCamera(Transform parent, PathOSWindow window)
        {
            GameObject sgo = PrefabUtility.InstantiatePrefab(screenshotCameraPrefab, parent) as GameObject;
            window.SetScreenshotCameraReference(sgo.GetComponent<ScreenshotManager>());
            return sgo;
        }

        private LBSGeneratedSimulation GenerateSimulationTile
            (Transform parent, LBSGenerator3DSettings settings, SimulationTile tile, int floor)
        {
            GameObject currInstance;
            Vector3 scale = settings.scale;

            // Player / Agent
            if (tile.Tag != null && tile.Tag.Label == "Player")//"PathOSAgent")
            {
                // Instancing
                currInstance = PrefabUtility.InstantiatePrefab(agentPrefab, parent) as GameObject;

                // Set position
                currInstance.transform.position = new Vector3((tile.X - 0.5f) * scale.x, floor * scale.y, (tile.Y - 0.5f) * scale.z);

                // Copy player camera component
                var player = GameObject.FindFirstObjectByType<CharacterController>();
                if (player is not null)
                {
                    PathOSAgentEyes eyesComp = currInstance.GetComponent<PathOSAgentEyes>();
                    eyesComp.cam.gameObject.SetActive(false);
                    GameObject camObj = player.GetComponentInChildren<Camera>().gameObject;
                    GameObject camClone = GameObject.Instantiate(camObj, eyesComp.transform);
                    eyesComp.cam = camClone.GetComponent<Camera>();
                }

                // Add simulation component
                LBSGeneratedSimulation pGenComp = currInstance.AddComponent<LBSGeneratedSimulation>();
                return pGenComp;
            }
            // Wall
            else if (tile.Tag != null && tile.Tag.Label == "Wall")
            {
                // Instancing
                currInstance = PrefabUtility.InstantiatePrefab(wallPrefab, parent) as GameObject;

                // Set scale
                currInstance.transform.localScale = new Vector3(scale.x, scale.y, scale.z);

                // Config NavMeshModifier
                NavMeshModifier modifier = currInstance.AddComponent<NavMeshModifier>();
                modifier.ignoreFromBuild = false;
                modifier.overrideArea = true;
                modifier.area = NavMesh.GetAreaFromName("Not Walkable");
            }
            else
            {
                // Instancing
                currInstance = PrefabUtility.InstantiatePrefab(elementPrefab, parent) as GameObject;
            }

            // Renderer config
            MeshRenderer currRenderer = currInstance.GetComponentInChildren<MeshRenderer>();
            Material originalMaterial = currRenderer.sharedMaterial;
            Material currMaterial = new Material(originalMaterial);
            currRenderer.material = currMaterial;
            currRenderer.enabled = false;

            // Set Position
            currInstance.transform.position = new Vector3((tile.X - 0.5f) * scale.x, floor * scale.y, (tile.Y - 0.5f) * scale.z);

            // Add simulation component
            LBSGeneratedSimulation genComp = currInstance.AddComponent<LBSGeneratedSimulation>();
            if (tile.LockedDoorPOI)
                genComp.hideAtStart = true;
            return genComp;
        }

        private GameObject GenerateNavMesh(List<(SimulationTile, GameObject)> walls)
        {
            // Si existe un NavMesh, evita generar otro.
            //NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            //if (triangulation.vertices.Length == 0 && triangulation.indices.Length == 0)
            //{
            //    Debug.LogWarning("Ya existe un NavMesh! No se generara uno nuevo.");
            //    return;
            //}

            // Interior Layers: GameObjects
            List<GameObject> interiorLayerGameObjects = 
                //GameObject.FindObjectsOfType<GameObject>()
                GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(
                obj => obj.transform.childCount == 2 &&
                obj.transform?.GetChild(0).name == "Schema" &&
                obj.transform?.GetChild(1).name == "Schema outside").ToList();

            // Exterior Layers: GameObjects
            List<GameObject> exteriorLayerGameObjects =
                            GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                            .Where(obj => obj.name == "Navigable").ToList();

            // Si no se encuentra, advierte.
            if (interiorLayerGameObjects.Count == 0 && exteriorLayerGameObjects.Count == 0)
            {
                Debug.LogWarning("Ninguna instancia de Exterior o Interior Layer encontrada. No se generara un NavMesh.");
                return null;
            }

            // Crea padre temporal para usarlos en un solo mesh
            GameObject tempParent = new GameObject();
            tempParent.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // NavMeshSurface: Agrega componente de superficie y limita su efecto a los hijos de tempParent.
            // NOTA***: Producto de la potencial existencia de meshes sin acceso de lectura, usamos, en vez, los colliders.
            // Asi se evitan excepciones al realizar "re-Bakes" en Play Mode (ej.: Con obstaculos dinamicos)
            var surface = tempParent.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

            // Arreglos donde guardar padres viejos para la posterior reinsertacion
            GameObject[] interiorOldParents = new GameObject[interiorLayerGameObjects.Count];
            GameObject[] exteriorOldParents = new GameObject[exteriorLayerGameObjects.Count];
            GameObject[] wallsOldParents = new GameObject[walls.Count];

            // Interior Layers: Nuevo padre temporal
            for (int i = 0; i < interiorLayerGameObjects.Count; i++)
            {
                interiorOldParents[i] = interiorLayerGameObjects[i].transform.parent?.gameObject;
                // Agrega objetos al padre temporal
                interiorLayerGameObjects[i].transform.parent = tempParent.transform;
            }
            // Exterior Layers: Nuevo padre temporal
            for (int i = 0; i < exteriorLayerGameObjects.Count; i++)
            {
                exteriorOldParents[i] = exteriorLayerGameObjects[i].transform.parent?.gameObject;
                // Agrega objetos al padre temporal
                exteriorLayerGameObjects[i].transform.parent = tempParent.transform;
            }
            // Muros: Nuevo padre temporal
            for (int i = 0; i < walls.Count; i++)
            {
                wallsOldParents[i] = walls[i].Item2.transform.parent?.gameObject;
                // Agrega objetos al padre temporal
                walls[i].Item2.transform.parent = tempParent.transform;
            }

            // Si un objeto recolectado no tiene un Collider, pero renderiza (MeshRenderer), se
            // le asigna temporalmente un BoxCollider.
            // ***Los objetos del modulo de exteriores, por defecto, no vienen con Colliders.
            // ***Para ello se usa esta seccion (2024-12-16).
            var doNotHaveColliderList = new List<GameObject>();
            var totalList = new List<GameObject>();
            totalList.AddRange(interiorLayerGameObjects);
            totalList.AddRange(exteriorLayerGameObjects);
            //Debug.Log("TOTAL GAMEOBJECTS: " + totalList.Count);
            foreach (var obj in totalList)
            {
                if (obj.GetComponentsInChildren<MeshRenderer>().Length > 0 && obj.GetComponentsInChildren<Collider>().Length == 0)
                {
                    var currMeshPlusChildren = obj.GetComponentsInChildren<MeshRenderer>();
                    foreach(var mesh in currMeshPlusChildren)
                    {
                        mesh.gameObject.AddComponent<BoxCollider>();
                        doNotHaveColliderList.Add(mesh.gameObject);
                    }
                }
            }

            // Genera NavMesh (Bake)
            surface.BuildNavMesh();

            // Remover colliders temporales
            int meshCount = doNotHaveColliderList.Count;
            //Debug.Log("CREATED COLLIDERS: " +  meshCount);
            for (int i = 0; i < meshCount; i++)
            {
                GameObject.DestroyImmediate(doNotHaveColliderList[i].GetComponent<BoxCollider>());
            }

            // Interior Layers: Reasigna padre original
            for (int i = 0; i < interiorLayerGameObjects.Count; i++)
            {
                interiorLayerGameObjects[i].transform.parent = interiorOldParents[i]?.transform;
            }
            // Exterior Layers: Reasigna padre original
            for (int i = 0; i < exteriorLayerGameObjects.Count; i++)
            {
                exteriorLayerGameObjects[i].transform.parent = exteriorOldParents[i]?.transform;
            }
            // Muros: Reasigna padre original
            for (int i = 0; i < walls.Count; i++)
            {
                walls[i].Item2.transform.parent = wallsOldParents[i]?.transform;
            }

            // Padre temporal cambia de nombre (pasa a contener unicamente el navmesh)
            tempParent.name = "NavMeshSurface";

            return tempParent;
        }

        // [GABO DEBUG] Generador de prueba hecho originalmente para probar colocacion de elementos
        private GameObject SimpleBoxGenerate(LBSLayer layer, LBSGenerator3DSettings settings)
        {
            // Variables
            SimulationModule module = layer.GetModule<SimulationModule>();
            var tiles = module.GetTiles();
            var scale = settings.scale;

            // Obtiene (o crea) instancia de PathOSWindow
            PathOSWindow window = EditorWindow.GetWindow(typeof(PathOSWindow), false, "PathOS+", false) as PathOSWindow;

            // Objeto contenedor padre
            GameObject parent = new GameObject("PathOS+ Tags");
            // Prefab
            elementPrefab = Resources.Load<GameObject>("Prefabs/BoxWithTexture");
            // GameObject List
            List<GameObject> boxes = new List<GameObject>();

            foreach (SimulationTile tile in tiles)
            {
                // Instanciar prefab
#if UNITY_EDITOR
                GameObject currInstance = PrefabUtility.InstantiatePrefab(elementPrefab) as GameObject;
#else
                Debug.LogError("Attempting to use PathOSRuleGenerator class outside of Editor!"); return null;
#endif
                // Agregar icono del Tag asociado a este tile como textura al cubo
                MeshRenderer currRenderer = currInstance.GetComponentInChildren<MeshRenderer>();
                Material originalMaterial = currRenderer.sharedMaterial;
                Material currMaterial = new Material(originalMaterial);
                //currMaterial.SetTexture("_MainTex", tile.Tag.Icon);
                currRenderer.material = currMaterial;

                // Setear posicion
                currInstance.transform.position = settings.position +
                                                  new Vector3(tile.X * scale.x, 0, tile.Y * scale.y)
                                                  - new Vector3(scale.x, 0, scale.y) / 2f; // GABO TODO: Necesario ??? Basado en PopulationRuleGenerator.
                boxes.Add(currInstance);
            }

            if (boxes.Count > 0)
            {
                // Obtener posicion planar promedio de las cajas, y altura del objeto mas bajo.
                var x = boxes.Average(o => o.transform.position.x);
                var y = boxes.Min(o => o.transform.position.y);
                var z = boxes.Average(o => o.transform.position.z);
                // Asignar esta posicion al objeto contenedor padre
                parent.transform.position = new Vector3(x, y, z);
            }

            foreach (var box in boxes)
            {
                box.transform.parent = parent.transform;
            }

            // Ya unidos los objetos hijos con padre, trasladar segun Settings
            // GABO TODO: No es esto un error? Basado en PopulationRuleGenerator.
            parent.transform.position += settings.position;

            return parent;
        }
        #endregion
    }
}
