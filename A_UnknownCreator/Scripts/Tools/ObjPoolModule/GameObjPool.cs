using UnityEngine;
namespace UnknownCreator.Modules
{
    public class GameObjPool : ObjPoolBase<GameObject>
    {
        public override string poolName => prefabName;

        private GameObject gameObject;

        private string prefabName;

        private bool isSetRoot, isActive;

        public GameObjPool(ObjPoolInfo info, string prefabName, bool isSetRoot, bool isActive)
        : base(info)
        {
            this.prefabName = prefabName;
            this.isSetRoot = isSetRoot;
            this.isActive = isActive;
            gameObject = UnityGlobals.LoadSync<GameObject>(this.prefabName);
        }

        protected override GameObject OnCreate()
        {
            var obj = UnityEngine.Object.Instantiate(gameObject);
            obj.SetActive(isActive);
            if (isSetRoot) Mgr.GPool.SetRoot(obj, true);
            return obj;
        }

        protected override void OnPop(GameObject obj)
        {
            obj.SetActive(isActive);
        }

        protected override void OnPreStored(GameObject obj)
        {
            obj.SetActive(isActive);
            if (isSetRoot) Mgr.GPool.SetRoot(obj,true);
        }

        protected override void OnRelease(GameObject obj)
        {
            Mgr.GPool.SetRoot(obj, true);
            obj.SetActive(false);
        }

        protected override void OnClear(GameObject obj)
        {
            UnityEngine.Object.Destroy(obj);
        }

        protected override void OnPoolDestroy()
        {
            if (gameObject != null) UnityGlobals.Release(gameObject);
            gameObject = null;
        }
    }
}
