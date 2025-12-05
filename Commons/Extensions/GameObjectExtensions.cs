using UnityEngine;

namespace ISILab.Commons.Extensions
{
    public static class GameObjectExtensions
    {
        public static void SetParent(this GameObject gameObjct, GameObject other)
        {
            gameObjct.transform.parent = other.transform;
        }
    }
}