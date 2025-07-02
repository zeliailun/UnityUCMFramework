using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnknownCreator.Modules
{
    public sealed class LoadSceneMgr : ILoadSceneMgr
    {
        private List<LoadingScene> loadSceneList = new();

        public string enterSceneName { get; private set; }

        public int enterSceneID { get; private set; }

        private LoadingScene loadingScene;

        //private LoadSceneMgr() { }

        void IDearMgr.WorkWork()
        {
            loadSceneList ??= new();
        }

        void IDearMgr.UpdateMGR()
        {
            for (int i = loadSceneList.Count - 1; i >= 0; i--)
            {
                loadingScene = loadSceneList[i];
                if (loadingScene.isLoaded)
                {
                    loadSceneList.RemoveAt(i);
                    Mgr.RPool.Release(loadingScene);
                    loadingScene = null;
                    continue;
                }
                loadSceneList[i].Update();
            }
        }

        void IDearMgr.DoNothing()
        {
            for (int i = 0; i < loadSceneList.Count; i++)
            {
                Mgr.RPool.Release(loadSceneList[i]);
            }
            loadSceneList = null;
        }

        public void EnterLoading(string loadingSceneName, string nextSceneName)
        {
            enterSceneName = nextSceneName;
            SceneManager.LoadScene(loadingSceneName);
        }

        public void EnterLoading(int loadingScene, int nextScene)
        {
            enterSceneID = nextScene;
            SceneManager.LoadScene(loadingScene);
        }

        public void LoadSceneByProgress(string sceneName, Func<float, bool> onSceneProgress, Action onSceneLoaded)
        {
            var value = Mgr.RPool.Load<LoadingScene>();
            value.Start(sceneName, onSceneProgress, onSceneLoaded);
            loadSceneList.Insert(0, value);
        }

        public void LoadSceneByProgress(int sceneID, Func<float, bool> onSceneProgress, Action onSceneLoaded)
        {
            var value = Mgr.RPool.Load<LoadingScene>();
            value.Start(sceneID, onSceneProgress, onSceneLoaded);
            loadSceneList.Insert(0, value);
        }

        public void LoadNextScene(bool isCyclical)
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (buildIndex > SceneManager.sceneCountInBuildSettings - 1 && isCyclical)
                buildIndex = 0;
            else
                return;
            SceneManager.LoadScene(buildIndex);
        }

        public void LoadPreviousScene(bool isCyslical)
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex - 1;
            if (buildIndex < 0 && isCyslical)
                buildIndex = SceneManager.sceneCountInBuildSettings - 1;
            else
                return;
            SceneManager.LoadScene(buildIndex);
        }
    }
}