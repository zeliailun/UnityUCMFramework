using System;
using System.Collections.Generic;
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

        public static void Clear()
        {
            foreach (var item in uiDict)
            {
                Mgr.RPool.Release(item);
            }
            uiDict.Clear(); 
            prDict.Clear();
            uiNameDict.Clear();
            builders.Clear();
        }

        public static UITKBuilder GetBuilder(string idName)
        {
            if (prDict.TryGetValue(idName, out var result))
            {
                return result;
            }
            return null;
        }

        public static void AddBuilder(UITKBuilder uid)
        {
            if (!prDict.TryGetValue(uid.idName, out _))
            {
                prDict.Add(uid.idName, uid);
                builders.Add(uid);
            }
        }

        public static void RemoveBuilder(UITKBuilder uid)
        {
            if (!prDict.Remove(uid.idName)) return;

            builders.Remove(uid);

            if (!uiNameDict.TryGetValue(uid.idName, out var uiNames)) return;

            foreach (var uiName in uiNames)
            {
                if (uiDict.TryGetValue(uiName, out var uiView))
                {
                    Mgr.RPool.Release(uiView);
                    uiDict.Remove(uiName);
                }
            }

            uiNameDict.Remove(uid.idName);
        }

        public static void OpenUI(UITKOpenInfo info)
        {
            if (!prDict.TryGetValue(info.prName, out var builder)) return;

            var visualElement = builder.root.Q<VisualElement>(info.uiName);
            if (visualElement == null)
            {
                UCMDebug.Log("无法找到面板>" + info.uiName);
                return;
            }

            if (!uiDict.TryGetValue(info.uiName, out var uiView))
            {
                uiView = Mgr.RPool.Load<UITKOpenHideView>();
                uiView.Init(builder);
                uiView.Show(info);
                uiDict[info.uiName] = uiView;

                if (!uiNameDict.TryGetValue(info.prName, out _))
                    uiNameDict[info.prName] = new List<string>();

                uiNameDict[info.prName].Add(info.uiName);
            }
            else
            {
                uiView.Show(info);
            }
        }

        public static void HideUI(UITKHideInfo info)
        {
            if (!prDict.TryGetValue(info.prName, out var builder)) return;

            var visualElement = builder.root.Q<VisualElement>(info.uiName);
            if (visualElement == null)
            {
                UCMDebug.Log("无法找到面板>"+ info.uiName);
                return;
            }

            if (uiDict.TryGetValue(info.uiName, out var uiView))
            {
                uiView.Hide(info);
            }
            else
            {
                uiView = Mgr.RPool.Load<UITKOpenHideView>();
                uiView.Init(builder);
                uiView.Hide(info);
                uiDict[info.uiName] = uiView;

                if (!uiNameDict.TryGetValue(info.prName, out _))
                    uiNameDict[info.prName] = new List<string>();

                uiNameDict[info.prName].Add(info.uiName);
            }
        }

        public static void ChangeUI<T>(UITKChangeInfo info) where T : UITKStateBase, new()
        {
            var sm = GetBuilder(info.prName).hbsm.GetHBSM(info.uiSM);
            if (info.isNull)
                sm.ChangeNullState();
            else
                sm.ChangeState<T>(true);
        }

        public static void EnableUIComp<T>(string uidName) where T : UITKCompStateBase, new()
        {
            var sm = UITKMgr.GetBuilder(uidName).hbsm.GetComp<T>();
            sm.enable = true;
        }

        public static void DisableUIComp<T>(string uidName) where T : UITKCompStateBase, new()
        {
            var sm = UITKMgr.GetBuilder(uidName).hbsm.GetComp<T>();
            sm.enable = false;
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

