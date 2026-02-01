#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace UnknownCreator.Modules
{

    internal static class EditorTools
    {


        [MenuItem("GameObject/UnknownCreator/EmptyUnit", false, 0)]
        public static void CreatePlayer()
        {
            var unit = new GameObject("Unit");
            Undo.RegisterCreatedObjectUndo(unit, "Create Unit");

            var model = new GameObject(UnitGlobals.Model);
            Undo.RegisterCreatedObjectUndo(model, "Create Unit Model");

            model.layer = 2;
            model.transform.SetParent(unit.transform);

            Selection.activeGameObject = unit;
        }



        [MenuItem("GameObject/UnknownCreator/CircleDrawer", false, 1)]
        public static void CreateCircleDrawer()
        {
            var unit = new GameObject("CircleDrawer");
            unit.AddComponent<CircleDrawer>();
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                Vector3 spawnPos = sceneView.pivot;

                // 射线检测地面
                Ray ray = new Ray(spawnPos + Vector3.up * 50f, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    spawnPos = hit.point; // 命中地面
                }

                unit.transform.position = spawnPos;
            }

            Selection.activeGameObject = unit;
            Undo.RegisterCreatedObjectUndo(unit, "Create CircleDrawer");
        }


        [MenuItem("GameObject/UnknownCreator/CopyPath %Q")]
        public static void CopyPath()
        {
            var obj = GetTarget<GameObject>();
            if (obj == null)
            {
                EditorUtility.DisplayDialog("提示", "没选择对象不能复制", "确定");
                return;
            }

            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent)
            {
                path = string.Format("{0}/{1}", parent.name, path);
                parent = parent.parent;
            }

            Debug.Log(path);
            TextEditor te = new TextEditor
            {
                text = path
            };
            te.SelectAll();
            te.Copy();
        }

        [MenuItem("Assets/UnknownCreator/SOToJson", false, 0)]
        public static void SOToJson()
        {
            var targetSO = GetTarget<ScriptableObject>();
            if (targetSO == null) return;

            string path = EditorUtility.SaveFilePanel("保存 JSON", "", targetSO.name + ".json", "json");
            if (string.IsNullOrEmpty(path))
                return;
            var data = new Dictionary<string, object>();
            FieldInfo[] fields = targetSO.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.DeclaringType != typeof(ScriptableObject))
                    data[field.Name] = field.GetValue(targetSO);
            }

            string json = JsonMapper.ToJson(data);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            UCMDebug.Log("转换完成: " + path);
        }



        [MenuItem("Assets/UnknownCreator/创建bcvox", false, 1)]
        private static void CopyAndRename()
        {
            // ===== 校验阶段 =====
            if (Selection.objects.Length != 1)
                return;

            UnityEngine.Object obj = Selection.activeObject;
            string srcPath = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(srcPath))
                return;

            // 排除文件夹
            if (AssetDatabase.IsValidFolder(srcPath))
                return;

            // 只允许 .vox
            if (!srcPath.EndsWith(".vox", System.StringComparison.OrdinalIgnoreCase))
                return;

            // ===== 执行阶段 =====
            string directory = Path.GetDirectoryName(srcPath);
            string filename = Path.GetFileNameWithoutExtension(srcPath);
            string targetPath = Path.Combine(directory, filename + ".bcvox");

            targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);

            File.Copy(srcPath, targetPath);
            AssetDatabase.Refresh();
        }


        public static void FocusAbilityFromScript()
        {
            // 获取当前选中的 C# 脚本
            var obj = Selection.activeObject as MonoScript;
            if (obj == null) return;

            string scriptName = obj.name; // 脚本类名
                                          // 这里假设 AbilityCfgSO 名称和脚本类名一致
            var cfg = AssetDatabase.FindAssets(scriptName + " t:AbilityCfgSO")
                .Select(guid => AssetDatabase.LoadAssetAtPath<AbilityCfgSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();

            if (cfg != null)
            {
                // 打开你的编辑器并选中该配置
                var wnd = EditorWindow.GetWindow<GamePlayEditor>();
                wnd.titleContent = new GUIContent("GamePlayEditor");
                wnd.Show();
                wnd.Focus();

                // 选中对应的配置
                var method = typeof(GamePlayEditor).GetMethod("SelectSO", BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(wnd, new object[] { cfg });
            }
            else
            {
                EditorUtility.DisplayDialog("提示", $"找不到对应的 Ability 配置: {scriptName}", "确定");
            }
        }


        private static T GetTarget<T>() where T : UnityEngine.Object
        {
            UnityEngine.Object[] objs = Selection.objects;
            if (objs.Length < 1)
                return default;

            T obj = objs[0] as T;
            if (obj == null)
                return default;

            return obj;
        }

    }

}
#endif
