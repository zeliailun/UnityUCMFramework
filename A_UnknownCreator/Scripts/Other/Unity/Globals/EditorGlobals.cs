#if UNITY_EDITOR
using UnityEngine;
using System.Diagnostics;
using System;
using UnityEditor;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public static class EditorUtils
    {
        public static string GetHierarchyPath(GameObject obj)
        {
            if (obj == null)
            {
                UCMDebug.LogWarning("GameObject is null!");
                return "";
            }

            Transform objTransform = obj.transform;
            string path = objTransform.name;

            while (objTransform.parent != null)
            {
                objTransform = objTransform.parent;
                path = objTransform.name + "/" + path;
            }

            return path;
        }

        public static T GetAsset<T>(string name)
        where T : class
        {
            var guid = AssetDatabase.FindAssets(name);
            if (guid.Length <= 0) return null;

            foreach (var item in guid)
            {
                var asset = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(item));

                for (int i = 0; i < asset.Length; i++)
                {
                    if (asset[i] is not null and T)
                    {
                        return asset[i] as T;
                    }
                }
            }

            return null;
        }

        public static T GetAsset<T>(string filter, string[] searchInFolders)
        where T : class
        => GetAsset<T>(filter, searchInFolders);

        public static List<T> GetAllSO<T>()
        where T : ScriptableObject
        {
            List<T> list = new();
            string[] assetPaths = AssetDatabase.FindAssets("t:ScriptableObject", new string[] { "Assets" });
            foreach (string assetPath in assetPaths)
            {
                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(assetPath));
                if (so is not null and T)
                    list.Add((T)so);
            }
            return list;
        }


        public static T Create<T>(string name) where T : ScriptableObject
        {

            return (T)Create(typeof(T).FullName, name);
        }

        public static ScriptableObject Create(string className, string name)
        {
            ScriptableObject obj = ScriptableObject.CreateInstance(className);
            var path = EditorUtility.SaveFilePanelInProject("资源创建", name + ".asset", "asset", "");
            if (string.IsNullOrEmpty(path))
            {
                UCMDebug.Log("取消创建文件：" + "【类型】" + className + "【名称】" + name);
                return null;
            }
            AssetDatabase.CreateAsset(obj, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UCMDebug.Log("成功创建文件：" + "【类型】" + className + "【名称】" + name);
            return obj;
        }

        public static void TestCodeRunTime(Action action, string title)
        {
            Stopwatch sw = new();
            sw.Start();
            action.Invoke();
            sw.Stop();
            UCMDebug.Log(title + "：" + "【" + sw.Elapsed.TotalMilliseconds + "毫秒】");
            UCMDebug.Log(title + "：" + "【" + sw.Elapsed.TotalSeconds + "秒】");
        }
    }
}
#endif