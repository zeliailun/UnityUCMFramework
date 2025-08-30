using System;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public class LoadingScene : IReference
    {
        private Func<float, bool> onSceneProgress;
        private Action onSceneLoaded;
        private AsyncOperation operation;

        public bool isLoaded { get; private set; }

        private void Start(Func<float, bool> onSceneProgress, Action onSceneLoaded)
        {
            this.onSceneProgress = onSceneProgress;
            this.onSceneLoaded = onSceneLoaded;
            isLoaded = false;
        }

        public void Start(string sceneName, Func<float, bool> onSceneProgress, Action onSceneLoaded)
        {
            Start(onSceneProgress, onSceneLoaded);
            operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;
        }

        public void Start(int sceneID, Func<float, bool> onSceneProgress, Action onSceneLoaded)
        {
            Start(onSceneProgress, onSceneLoaded);
            operation = SceneManager.LoadSceneAsync(sceneID);
            operation.allowSceneActivation = false;
        }

        public void Update()
        {
            if (operation == null) return;

            if (onSceneProgress?.Invoke(operation.progress) ?? true)
            {
                if (operation.progress >= 0.9F)
                {
                    onSceneLoaded?.Invoke();
                    operation.allowSceneActivation = true;
                    isLoaded = true;
                }
            }

        }


        void IReference.ObjRelease()
        {
            operation = null;
            onSceneProgress = null;
            onSceneLoaded = null;
        }
    }
}