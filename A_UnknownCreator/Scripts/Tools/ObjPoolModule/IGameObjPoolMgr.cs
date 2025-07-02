using UnityEngine;
namespace UnknownCreator.Modules
{
    public interface IGameObjPoolMgr : IDearMgr
    {
        string rootName { get; set; }
        int poolCount { get; }
        public GameObject Load(string prefabName, bool isSetRoot, bool isActive);
        void Release(string prefabName, GameObject go);
        void ReleaseNewGameObject(string name, GameObject go);
        void DestroyPool(string prefabName);
        void ClearAll();
        void DestroyAll();
        ObjPoolBase<GameObject> CreatePool(string prefabName, bool isSetRoot, bool isActive, ObjPoolInfo info);
        (GameObject, bool) GetNewGameObject(string name);
        void SetRoot(GameObject obj, bool worldPositionStays);
        void SetRoot(Transform obj, bool worldPositionStays);
        bool HasObject(GameObject obj,string name);
        void Preload(string name, GameObject go, int count, ObjPoolInfo info);
    }
}