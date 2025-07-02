using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace UnknownCreator.Modules
{
    public static class UITKMgr
    {
        private static Dictionary<string, UITKOpenHideView> uiDict = new();
        private static Dictionary<string, List<string>> uiNameDict = new();
        private static Dictionary<string, UITKBuilder> uidDict = new();
        private static List<UITKBuilder> uids = new();

        public static Action<string, string> OnUIOpen;
        public static Action<string, string> OnUIHide;

        public static UITKBuilder GetUID(string idName)
        {
            if (uidDict.TryGetValue(idName, out var result))
            {
                return result;
            }
            return null;
        }

        public static void AddUID(UITKBuilder uid)
        {
            if (!uidDict.TryGetValue(uid.idName, out _))
            {
                uidDict.Add(uid.idName, uid);
                uids.Add(uid);
            }
        }

        public static void RemoveUID(UITKBuilder uid)
        {
            if (!uidDict.Remove(uid.idName)) return;

            uids.Remove(uid);

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
            if (!uidDict.TryGetValue(info.uidName, out var builder)) return;

            var visualElement = builder.uid.rootVisualElement.Q<VisualElement>(info.uiName);
            if (visualElement == null) return;

            if (!uiDict.TryGetValue(info.uiName, out var uiView))
            {
                uiView = Mgr.RPool.Load<UITKOpenHideView>();
                uiView.Init(visualElement);
                uiView.Show(info);
                uiDict[info.uiName] = uiView;

                if (!uiNameDict.TryGetValue(info.uidName, out _))
                    uiNameDict[info.uidName] = new List<string>();

                uiNameDict[info.uidName].Add(info.uiName);
            }
            else
            {
                uiView.Show(info);
            }
        }

        public static void HideUI(UITKHideInfo info)
        {
            if (!uidDict.TryGetValue(info.uidName, out var builder)) return;

            var visualElement = builder.uid.rootVisualElement.Q<VisualElement>(info.uiName);
            if (visualElement == null) return;

            if (uiDict.TryGetValue(info.uiName, out var uiView))
            {
                uiView.Hide(info);
            }
            else
            {
                uiView = Mgr.RPool.Load<UITKOpenHideView>();
                uiView.Init(visualElement);
                uiView.Hide(info);
                uiDict[info.uiName] = uiView;

                if (!uiNameDict.TryGetValue(info.uidName, out _))
                    uiNameDict[info.uidName] = new List<string>();

                uiNameDict[info.uidName].Add(info.uiName);
            }
        }

        public static void ChangeUI<T>(UITKChangeInfo info) where T : UITKStateBase, new()
        {
            var sm = UITKMgr.GetUID(info.uidName).hbsm.GetHBSM(info.uiSM);
            if (info.isNull)
                sm.ChangeNullState();
            else
                sm.ChangeState<T>(true);
        }

        public static void EnableUIComp<T>(string uidName) where T : UITKCompStateBase, new()
        {
            var sm = UITKMgr.GetUID(uidName).hbsm.GetComp<T>();
            sm.enable = true;
        }

        public static void DisableUIComp<T>(string uidName) where T : UITKCompStateBase, new()
        {
            var sm = UITKMgr.GetUID(uidName).hbsm.GetComp<T>();
            sm.enable = false;
        }
    }

    [Serializable]
    public struct UITKChangeInfo
    {
        public string uidName;
        public string uiSM;
        public bool isNull;
    }
}

