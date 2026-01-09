using ISILab.Commons;
using ISILab.Commons.Extensions;
using ISILab.LBS.Macros;
using ISILab.LBS.Modules;
using ISILab.LBS.Plugin.Components.Bundles;
using ISILab.LBS.Plugin.Components.Data;
using ISILab.LBS.Plugin.Components.Data.Tessellation.TileMap;
using ISILab.LBS.Plugin.Core.Settings;
using ISILab.LBS.Plugin.Internal;
using LBS.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [System.Serializable]
    [RequieredModule(typeof(TileMapModule),
        typeof(ConnectedTileMapModule),
        typeof(SectorizedTileMapModule),
        typeof(ConnectedZonesModule))]
    public class SchemaRuleGenerator : LBSGeneratorRule
    {
        #region FIELDS
        [JsonRequired]
        private float deltaWall = 1f;
        #endregion

        #region INTERNAL FIELDS
        [JsonIgnore]
        private TileMapModule tilesMod;
        [JsonIgnore]
        private ConnectedTileMapModule connectedTilesMod;
        [JsonIgnore]
        private SectorizedTileMapModule zonesMod;
        [JsonIgnore]
        private LBSGenerator3DSettings settings;
        #endregion

        #region PPROPERTIES
        [JsonIgnore]
        private List<Vector2Int> Dirs => Directions.Bidimencional.Edges;
        [JsonIgnore]
        private List<Vector2Int> DirDiags => Directions.Bidimencional.Diagonals;
        #endregion

        #region CONSTRUCTORS
        public SchemaRuleGenerator() { }

        // For template construction
        public SchemaRuleGenerator(string IconGuid, string name, Color colorTint) : base() { }

        #endregion

        #region METHODS
        public override object Clone()
        {
            return new SchemaRuleGenerator();
        }

        public override bool Equals(object obj)
        {
            SchemaRuleGenerator other = obj as SchemaRuleGenerator;

            if (other == null) return false;

            if (!this.deltaWall.Equals(other.deltaWall)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void Init(LBSLayer layer, LBSGenerator3DSettings settings)
        {
            this.tilesMod = layer.GetModule<TileMapModule>();
            this.connectedTilesMod = layer.GetModule<ConnectedTileMapModule>();
            this.zonesMod = layer.GetModule<SectorizedTileMapModule>();
            this.settings = settings;
        }

        public override List<Message> CheckViability(LBSLayer layer)
        {
            List<Message> msgs = new List<Message>();
            SectorizedTileMapModule zonesMod = layer.GetModule<SectorizedTileMapModule>();

            foreach (Zone zone in zonesMod.Zones)
            {
                if (zone.OutsideStyles.Count <= 0)
                {
                    msgs.Add(new Message(
                        Message.Type.Warning,
                        "La zona '" + zone + "' no contiene bundles de estilo para crear el outside."
                        ));
                }

                if (zone.InsideStyles.Count <= 0)
                {
                    msgs.Add(new Message(
                        Message.Type.Warning,
                        "La zona '" + zone + "' no contiene bundles de estilo para crear el inside."
                        ));
                }
            }

            return msgs;
        }

        /// <summary>
        /// Geenerate elements correspoding to center in the bundle provided
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="bundles"></param>
        /// <returns></returns>
        private GameObject GenerateCenters(GameObject pivot, List<Bundle> bundles)
        {
            // Get "Center" bundles 
            List<Bundle> currents = new List<Bundle>();
            foreach (Bundle bundle in bundles)
            {
                currents = bundle.GetChildrenByPositioning(Positioning.Center);
            }

            List<string> tags = currents
                .SelectMany(b => LBSAssetMacro.GetAllTagNames(b))
                .ToList();

            tags.RemoveDuplicates();

            for (int i = 0; i < tags.Count; i++)
            {
                List<Bundle> xx = currents.Where(b => LBSAssetMacro.BundleHasTag(b, tags[i])).ToList();

                // Get random bundle
                Bundle current = xx.Random();

                // Get random by weight
                GameObject pref = current.Assets.RandomRullete(a => a.probability).obj;

                // Create part
                GameObject obj = CreateObject(pref, pivot.transform);
                
                // Add ref component
                LBSGenerated generatedComponent = obj.AddComponent<LBSGenerated>();
                generatedComponent.BundleRef = current;
    
            }

            return pivot;
        }

        /// <summary>
        /// Generate edges of the tile based on the connections of the tile with its neighbors
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="bundles"></param>
        /// <param name="connections"></param>
        /// <returns></returns>
        private GameObject GenerateEdges(GameObject pivot, List<Bundle> bundles, List<string> connections)
        {
            // Get "Edge" bundles
            List<Bundle> currents = new List<Bundle>();
            foreach (Bundle bundle in bundles)
            {
                currents = bundle.GetChildrenByPositioning(Positioning.Edge);
            }

            for (var i = 0; i < connections.Count; i++)
            {
                // Get random bundle with respctive "connection tag"
                Bundle current = LBSAssetMacro.GetRandomBundleWithTag(currents, connections[i]);

                // check if current is valid
                if (current == null) continue;

                // Get random by weight
                GameObject pref = current.Assets.RandomRullete(a => a.probability).obj;

                // Create part
                GameObject obj = CreateObject(pref, pivot.transform);

                // Set rotation orientation
                if (i % 2 == 0)
                    obj.transform.rotation = Quaternion.Euler(0, (90 * (i - 1)) % 360, 0);
                else
                    obj.transform.rotation = Quaternion.Euler(0, (90 * (i - 3)) % 360, 0);

                // Set delta position
                obj.transform.position = new Vector3(
                    settings.scale.x / 2f * -obj.transform.forward.x,
                    0,
                    settings.scale.y / 2f * -obj.transform.forward.z) * deltaWall;
                
                // Add ref component
                LBSGenerated generatedComponent = obj.AddComponent<LBSGenerated>();
                generatedComponent.BundleRef = current;
            }

            return pivot;
        }

        /// <summary>
        /// Generate corner based on bundles provided
        /// </summary>
        /// <param name="pivot"></param>
        /// <param name="bundles"></param>
        /// <param name="tile"></param>
        /// <returns></returns>
        private GameObject GenerateCorners(GameObject pivot, List<Bundle> bundles, LBSTile tile)
        {
            List<Bundle> currents = new List<Bundle>();
            foreach (Bundle bundle in bundles)
            {
                currents = bundle.GetChildrenByPositioning(Positioning.Corner);
            }

            Bundle current = currents.Random();

            List<string> selfConnections = connectedTilesMod.GetConnections(tile);
            for (int i = 0; i < Dirs.Count; i++)
            {
                Vector2Int d1 = Dirs[i];
                Vector2Int d2 = Dirs[(i + 1) % Dirs.Count];

                // if directions are NOT empty continue
                if (!selfConnections[i].Equals("Empty") || !selfConnections[(i + 1)% Dirs.Count].Equals("Empty"))
                    continue;

                LBSTile neigth = tilesMod.GetTileNeighbor(tile, d1);
                LBSTile neigth2 = tilesMod.GetTileNeighbor(tile, d2);

                // if neigths are null continue
                if (neigth == null || neigth2 == null)
                    continue;

                // Get neigth connections
                List<string> neigthConnections = connectedTilesMod.GetConnections(neigth);
                List<string> neigthConnections2 = connectedTilesMod.GetConnections(neigth2);

                if (neigthConnections[(i + 1) % Dirs.Count] != "Empty" || neigthConnections2[i] != "Empty")
                {
                    // Get random by weight
                    GameObject pref = current.Assets.RandomRullete(a => a.probability).obj;
                    GameObject instance = CreateObject(pref, pivot.transform);

                    // Set delta position
                    Vector2Int dir = Dirs[i] + Dirs[(i + 1) % Dirs.Count];
                    instance.transform.position = new Vector3(
                        settings.scale.x / 2f * dir.x,
                        0,
                        settings.scale.y / 2f * dir.y) * deltaWall;

                    // Set rotation orientation
                    var rot = (i) % Dirs.Count();
                    instance.transform.rotation = Quaternion.Euler(0, -90 * (rot + 1), 0);
                    
                    // Add ref component
                    LBSGenerated generatedComponent = instance.AddComponent<LBSGenerated>();
                    generatedComponent.BundleRef = current;
                }

            }

            
            return pivot;
        }

        /// <summary>
        /// Generate in 3D the schema of the layer
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public override GeneratedGO Generate(LBSLayer layer, LBSGenerator3DSettings settings)
        {
            // Init values
            Init(layer, settings);

            // Get bundles
            List<Bundle> allBundles = LBSAssetsStorage.Instance.Get<Bundle>().ToList();
            List<Bundle> rootBundles = allBundles.Where(b => b.IsRoot()).ToList();

            // Create pivot
            GameObject mainPivot = new GameObject("Schema");

            List<GameObject> tiles = new List<GameObject>();
            foreach (LBSTile tile in tilesMod.Tiles)
            {
                if(tile == null) continue;
                // Get zone
                Zone zone = zonesMod.GetZone(tile);
                if(zone == null) continue;
                zone.AddPosition(tile.Position);
                // Get bundle from current tile
                List<Bundle> bundles = zone.GetInsideBundles();

                if (bundles.Count <= 0)
                {
                    Debug.LogWarning("[ISI Lab]: Could not finish generating zone '" + zone.ID + "'" +
                    " since it does not contain bundles defining its interior style");
                    continue;
                }

                // Get connections
                List<string> connections = connectedTilesMod.GetConnections(tile);

                //Generate tile
                GameObject tileObj = new GameObject(tile.Position.ToString());

                // Add pref part to pivot
                GenerateCenters(tileObj, bundles);
                GenerateEdges(tileObj, bundles, connections);
                GenerateCorners(tileObj, bundles, tile);

                Vector3 basePos = settings.position;
                Vector3 tilePos = new Vector3(tile.Position.x * settings.scale.x, 0, tile.Position.y * settings.scale.y);
                Vector3 delta = new Vector3(settings.scale.x, 0, settings.scale.y) / 2f;
                // Set General position
                tileObj.transform.position = basePos + tilePos - delta;

                // TODO: add component for gizmos here 

                // Set mainPivot as the parent of tileObj
                tiles.Add(tileObj);
            }

            List<GameObject> probes = new List<GameObject>();
            List<GameObject> lightVolumes = new List<GameObject>();
            foreach (Zone zone in zonesMod.Zones)
            {
                Vector2 zonePos = zonesMod.ZoneCentroid(zone);
                Vector2 zoneSize = zone.GetSize() * settings.scale;
                Vector3 basePos = settings.position;
                Vector3 tilePos = new Vector3(zonePos.x * settings.scale.x, 0, zonePos.y * settings.scale.y);
                Vector3 delta = new Vector3(settings.scale.x, 0, settings.scale.y) / 2f;
                Vector3 centerPos = basePos + tilePos - delta - Vector3.one;
                
                if (settings.reflectionProbe)
                {
                    // Set General position
                    GameObject probeObject = new GameObject("rf_" + zone.ID);
                    probeObject.AddComponent<ReflectionProbe>();
                    probeObject.transform.position = centerPos;
                    probes.Add(probeObject);

                    // Set size
                    ReflectionProbe rp = probeObject.GetComponent<ReflectionProbe>();
                    
                    rp.size = new Vector3(zoneSize.x, zoneSize.x, zoneSize.y);
                }

                if (settings.lightVolume)
                {
                    GameObject lightObject = new GameObject("lv_" + zone.ID);
                    LightProbeCubeGenerator light = lightObject.AddComponent<LightProbeCubeGenerator>();
                  //  lightObject.AddComponent<LightProbeGroup>();
                   // lightObject.AddComponent<BoxCollider>();
               
                    centerPos.y -= centerPos.y * settings.scale.y; // to be in the center of the room
                    lightObject.transform.position = centerPos;
                    lightVolumes.Add(lightObject);

                    BoxCollider boxCollider = lightObject.GetComponent<BoxCollider>();
                    boxCollider.isTrigger = true;
                    boxCollider.size = new Vector3(zoneSize.x, zoneSize.x*0.5f, zoneSize.y);
                    
                    light.transform.SetParent(lightObject.transform);
                }
              
            }
            
            if (tiles.Count <= 0)
            {
                return new GeneratedGO(mainPivot, "No tiles found");
            }
            
            // tiles
            var x = tiles.Average(t => t.transform.position.x);
            var y = tiles.Min(t => t.transform.position.y);
            var z = tiles.Average(t => t.transform.position.z);
            
            mainPivot.transform.position = new Vector3(x,y,z);

            foreach (GameObject tile in tiles ) 
            {
                tile.transform.parent = mainPivot.transform;
            }
            
            // reflection probes
            if (probes.Count > 0)
            {
                var px = probes.Min(t => t.transform.position.x);
                var py = probes.Min(t => t.transform.position.y);
                var pz = probes.Min(t => t.transform.position.z);

                GameObject probePivot = new GameObject("ReflectionProbes");
                probePivot.transform.position = new Vector3(px,py,pz);
                foreach (GameObject probe in probes ) 
                {
                    probe.transform.parent = probePivot.transform;
                }
                probePivot.transform.SetParent(mainPivot.transform);
            }
            
            // light volumes
            if (lightVolumes.Count > 0)
            {
                var px = lightVolumes.Min(t => t.transform.position.x);
                var py = lightVolumes.Min(t => t.transform.position.y);
                var pz = lightVolumes.Min(t => t.transform.position.z);
                
                GameObject lightVolPivot = new GameObject("LightVolumes");
                lightVolPivot.transform.position = new Vector3(px,py,pz);
                foreach (GameObject light in lightVolumes ) 
                {
                    light.transform.parent = lightVolPivot.transform;
                }
                
                lightVolPivot.transform.SetParent(mainPivot.transform);
                
            }

            // main
            mainPivot.transform.position += settings.position;
            
            return new GeneratedGO(mainPivot, null);
        }

        private GameObject CreateObject(GameObject pref, Transform pivot)
        {
#if UNITY_EDITOR
            GameObject obj = PrefabUtility.InstantiatePrefab(pref, pivot) as GameObject;
#else
            var obj =  GameObject.Instantiate(pref, pivot);
#endif
            return obj;
        }
        #endregion
    }
}