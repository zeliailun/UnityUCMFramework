#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public static class AssetsBuilder
    {

        [MenuItem("GameObject/UnknownCreator/EmptyUnit", false, 0)]
        private static void CreatePlayer()
        {
            var unit = new GameObject("Unit");
            var model = new GameObject(UnitGlobals.Model);
            model.SetLayer(2);
            model.transform.SetParent(unit.transform);
        }
    }
}
#endif