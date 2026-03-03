using UnityEngine;

namespace ISILab.LBS.Components
{
    public interface IBlueprintable
    {
        // meant to get objects in a given area within a component that get stored in a blueprint
        public BlueprintData[] GetObjects(Vector2Int StartPosition, Vector2Int EndPosition);
        // pass blueprint objects into a component which then loads them if the objeccts are valid for the component.
        public void LoadObjects(BlueprintData[] objects);
    }
}