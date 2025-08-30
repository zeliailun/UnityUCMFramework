using System;
using UnityEngine.SceneManagement;

namespace UnknownCreator.Modules
{
    public interface ILoadSceneMgr : IDearMgr
    {
        string enterSceneName { get; }
        int enterSceneID { get; }
        void EnterLoading(string loadingSceneName, string nextSceneName);
        void EnterLoading(int loadingScene, int nextScene);
        void LoadPreviousScene(bool isCyslical);
        void LoadNextScene(bool isCyclical);
        void LoadSceneByProgress(string sceneName, Func<float, bool> onSceneProgress, Action onSceneLoaded);
        void LoadSceneByProgress(int sceneID, Func<float, bool> onSceneProgress, Action onSceneLoaded);
    }
}