using UnityEngine;

namespace UnknownCreator.Modules
{

    public abstract class SLMono1Lazy<T> : MonoBehaviour where T : Component
    {

        private static bool quit = false;

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



    public abstract class SLMonoNormal<T> : MonoBehaviour where T : MonoBehaviour
    {

        private static T instance;

        public static T i => instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
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
            if (instance == null)
            {
                GameObject obj = new(typeof(T).Name);
                obj.AddComponent<T>();
            }
        }

        protected virtual void OnAwake() { }
    }
}