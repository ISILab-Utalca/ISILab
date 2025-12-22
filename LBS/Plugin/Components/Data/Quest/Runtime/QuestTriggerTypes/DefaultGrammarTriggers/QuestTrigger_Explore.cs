using ISILab.LBS.Components;
using UnityEngine;

namespace ISILab.LBS.Plugin.MapTools.Generators
{
    [QuestNodeActionTag("explore")]
    public class QuestTriggerExplore : QuestTrigger
    {
        [HideInInspector]
        public DataExplore dataExplore;
        [SerializeField]
        private int requiredExplored;

        public override void Init()
        {
            requiredExplored = dataExplore.findRandomPosition ? 1 : dataExplore.subdivisions;
            AssignExploredSubArea();
        }

        private void AssignExploredSubArea()
        {
            foreach (Transform child in transform)
            {
                QuestTriggerGoTo triggerGoto = child.GetComponent<QuestTriggerGoTo>();
                if (triggerGoto == null) continue;
                triggerGoto.OnTriggerCompleted -= ExploredMinus;
                triggerGoto.OnTriggerCompleted += ExploredMinus;
            }
        }

        protected override void SetData(QuestActionData data)
        {
            
            dataExplore = (DataExplore)data;
            if (dataExplore.findRandomPosition)
            {
                requiredExplored = 1;
                
                // get random position
                Vector3 randomPoint = GetRandomPointInBounds(BoxCollider);
                
                // New GameObject for this position
                GameObject triggerObject = new GameObject("RandomTrigger");
                triggerObject.transform.SetParent(transform);
                triggerObject.transform.position = randomPoint;
                
                QuestTriggerGoTo triggerGoto = triggerObject.AddComponent<QuestTriggerGoTo>();
                triggerGoto.Init();

            }
            else
            {
                requiredExplored = dataExplore.subdivisions;
                // Create subdivisions based on the main trigger
                Vector3 mainSize = BoxCollider.size;
                Vector3 mainCenter = BoxCollider.center;
                float subdivisionSizeX = mainSize.x / dataExplore.subdivisions;

                for (int i = 0; i < dataExplore.subdivisions; i++)
                {
                    GameObject triggerObject = new GameObject($"SubTrigger_{i}");
                    triggerObject.transform.SetParent(transform);
    
                    float offsetX = (i - (dataExplore.subdivisions - 1) / 2f) * subdivisionSizeX;
                    Vector3 localPosition = mainCenter + new Vector3(offsetX, 0, 0);
                    triggerObject.transform.localPosition = localPosition;

                    QuestTriggerGoTo triggerGoto = triggerObject.AddComponent<QuestTriggerGoTo>();
                    triggerGoto.Init();
                    Vector3 size = new Vector3(
                        Mathf.Abs(subdivisionSizeX), 
                        Mathf.Abs(mainSize.y), 
                        Mathf.Abs(mainSize.z)
                    );
                    triggerGoto.SetSize(size);

                }

            }
        }

        /// <summary>
        /// May want to do something with the trigger as well plus the reduction of the required explored area
        /// </summary>
        /// <param name="obj"></param>
        private void ExploredMinus(QuestTrigger obj)
        {
            requiredExplored--;
            obj.gameObject.SetActive(false);
        }

        protected void OnTriggerStay(Collider other)
        {
            if (IsPlayer(other))
            {
                TryComplete();
            }
        }

        private Vector3 GetRandomPointInBounds(BoxCollider collider)
        {
            Bounds bounds = collider.bounds;
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        protected override bool CanComplete()
        {
            return requiredExplored <= 0;
        }
        
        private void OnDestroy()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}