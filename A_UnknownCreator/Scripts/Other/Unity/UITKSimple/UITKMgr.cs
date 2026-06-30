using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    public static class UITKMgr
    {
        private static Dictionary<string, UITKOpenHideView> uiDict = new();
        private static Dictionary<string, List<string>> uiNameDict = new();
        private static Dictionary<string, UITKBuilder> prDict = new();
        private static List<UITKBuilder> builders = new();

        public static Action<string, string> OnUIOpen;
        public static Action<string, string> OnUIHide;
        public static Action<UITKBuilder> OnUIReload;

        private static string MakeKey(string prName, string uiName)
        {
            return prName + "/" + uiName;
        }

        public static void Clear()
        {
            foreach (var item in uiDict)
            {
                if (item.Value != null)
                    Mgr.RPool.Release(item.Value);
            }

            uiDict.Clear();
            prDict.Clear();
            uiNameDict.Clear();
            builders.Clear();

            OnUIOpen = null;
            OnUIHide = null;
            OnUIReload = null;
        }

        public static UITKBuilder GetBuilder(string idName)
        {
            if (string.IsNullOrWhiteSpace(idName))
                return null;

            if (prDict.TryGetValue(idName, out var result))
                return result;

            return null;
        }

        public static void AddBuilder(UITKBuilder uid)
        {
            if (uid == null || string.IsNullOrWhiteSpace(uid.idName))
                return;

            // 关闭 Domain Reload 后，可能存在旧 builder。
            // 同 idName 的 builder 以最新的为准。
            if (prDict.TryGetValue(uid.idName, out var oldBuilder))
            {
                if (oldBuilder == uid)
                    return;

                RemoveBuilder(oldBuilder);
            }

            prDict[uid.idName] = uid;

            if (!builders.Contains(uid))
                builders.Add(uid);
        }

        public static void RemoveBuilder(UITKBuilder uid)
        {
            if (uid == null || string.IsNullOrWhiteSpace(uid.idName))
                return;

            if (!prDict.TryGetValue(uid.idName, out var current))
                return;

            // 防止旧 builder 把新 builder 移除了。
            if (current != uid)
                return;

            prDict.Remove(uid.idName);
            builders.Remove(uid);

            if (!uiNameDict.TryGetValue(uid.idName, out var uiNames))
                return;

            for (int i = 0; i < uiNames.Count; i++)
            {
                string key = uiNames[i];

                if (uiDict.TryGetValue(key, out var uiView))
                {
                    if (uiView != null)
                        Mgr.RPool.Release(uiView);

                    uiDict.Remove(key);
                }
            }

            uiNameDict.Remove(uid.idName);
        }

        public static void OpenUI(UITKOpenInfo info)
        {
            if (!prDict.TryGetValue(info.prName, out var builder) || builder == null || builder.root == null)
                return;

            var visualElement = builder.root.Q<VisualElement>(info.uiName);
            if (visualElement == null)
            {
                UCMDebug.Log("无法找到面板>" + info.uiName);
                return;
            }

            string key = MakeKey(info.prName, info.uiName);

            if (!uiDict.TryGetValue(key, out var uiView) || uiView == null)
            {
                uiView = Mgr.RPool.Load<UITKOpenHideView>();
                uiView.Init(builder);

                uiDict[key] = uiView;

                if (!uiNameDict.TryGetValue(info.prName, out var list))
                {
                    list = new List<string>();
                    uiNameDict[info.prName] = list;
                }

                if (!list.Contains(key))
                    list.Add(key);
            }

            uiView.Show(info);
        }

        public static void HideUI(UITKHideInfo info)
        {
            if (!prDict.TryGetValue(info.prName, out var builder) || builder == null || builder.root == null)
                return;

            var visualElement = builder.root.Q<VisualElement>(info.uiName);
            if (visualElement == null)
            {
                UCMDebug.Log("无法找到面板>" + info.uiName);
                return;
            }

            string key = MakeKey(info.prName, info.uiName);

            if (!uiDict.TryGetValue(key, out var uiView) || uiView == null)
            {
                uiView = Mgr.RPool.Load<UITKOpenHideView>();
                uiView.Init(builder);

                uiDict[key] = uiView;

                if (!uiNameDict.TryGetValue(info.prName, out var list))
                {
                    list = new List<string>();
                    uiNameDict[info.prName] = list;
                }

                if (!list.Contains(key))
                    list.Add(key);
            }

            uiView.Hide(info);
        }

        public static void ChangeUI<T>(UITKChangeInfo info) where T : UITKStateBase, new()
        {
            var builder = GetBuilder(info.prName);
            if (builder == null || builder.hbsm == null)
                return;

            var sm = builder.hbsm.GetHBSM(info.uiSM);
            if (sm == null)
                return;

            if (info.isNull)
                sm.ChangeNullState();
            else
                sm.ChangeState<T>(true);
        }

        public static void EnableUIComp<T>(string uidName) where T : UITKCompStateBase, new()
        {
            var builder = GetBuilder(uidName);
            if (builder == null || builder.hbsm == null)
                return;

            var comp = builder.hbsm.GetComp<T>();
            if (comp == null)
                return;

            comp.enable = true;
        }

        public static void DisableUIComp<T>(string uidName) where T : UITKCompStateBase, new()
        {
            var builder = GetBuilder(uidName);
            if (builder == null || builder.hbsm == null)
                return;

            var comp = builder.hbsm.GetComp<T>();
            if (comp == null)
                return;

            comp.enable = false;
        }
    }

    [Serializable]
    public struct UITKChangeInfo
    {
        public string prName;
        public string uiSM;
        public bool isNull;
        public bool isAddSeq;
    }
}