#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    public class ToolsEditor : EditorWindow
    {
        private const string visualTreeName = "ToolsEditor";
        private VisualElement root => rootVisualElement;
        private VisualTreeAsset m_VisualTreeAsset;
        private ObjectField soFile;

        [MenuItem("UnknownCreator/ToolsEditor")]
        public static void Window()
        {
            ToolsEditor wnd = GetWindow<ToolsEditor>();
            wnd.titleContent = new GUIContent("ToolsEditor");
        }

        public void CreateGUI()
        {
            if (m_VisualTreeAsset == null)
                m_VisualTreeAsset = EditorUtils.GetAsset<VisualTreeAsset>(visualTreeName);

            root.Add(m_VisualTreeAsset.CloneTree());
            soFile = root.Q<ObjectField>("SOFile");
            root.Q<Button>("SOToJson").clicked += () => ConvertToJSON();


        }



        private void ConvertToJSON()
        {
            if (soFile.value == null) return;

            ScriptableObject targetSO = (ScriptableObject)soFile.value;
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
    }
}
#endif