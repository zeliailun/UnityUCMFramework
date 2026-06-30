using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnknownCreator.Modules
{

    public  abstract partial class SLMono1Lazy<T> : MonoBehaviour where T : Component
    {
        [AutoStaticsCleanup]
        private static bool quit = false;
        [AutoStaticsCleanup]
        private static T instance;

        public static T i
        {
            get
            {
                if (quit) return null;
                if (instance == null)
                {
                    GameObject obj = new(typeof(T).Name);
                    instance = obj.AddComponent<T>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        private void Awake()
        {
            quit = false;
            OnAwake();
        }

        private void OnDestroy()
        {
            quit = true;
            OnEnd();
        }

        protected virtual void OnAwake() { }

        protected virtual void OnEnd() { }
    }



    public abstract partial  class SLMonoNormal<T> : MonoBehaviour where T : MonoBehaviour
    {
        [AutoStaticsCleanup]
        public static T i { get; private set; }

        private void Awake()
        {
            if (i == null)
            {
                i = this as T;
                DontDestroyOnLoad(gameObject);
                OnAwake();
            }
            else
            {
                Object.Destroy(gameObject);
            }
        }

        public static void CreateSelf()
        {
            if (i == null)
            {
                GameObject obj = new(typeof(T).Name);
                obj.AddComponent<T>();
            }
        }

        protected virtual void OnAwake() { }


    }
}