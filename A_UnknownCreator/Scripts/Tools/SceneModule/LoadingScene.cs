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
            if (operation == null || isLoaded)
                return;

            var progress = Mathf.Clamp01(operation.progress / 0.9f);
            var allow = onSceneProgress?.Invoke(progress) ?? true;

            if (allow && operation.progress >= 0.9f)
                operation.allowSceneActivation = true;

            if (operation.isDone)
            {
                onSceneLoaded?.Invoke();
                isLoaded = true;
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