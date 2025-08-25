#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public static class UnityEditorGlobals
    {

        public static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property, bool enterChildren = false)
        {
            if (property == null) yield break;

            var copy = property.Copy();
            var next = property.Copy();
            if (!next.NextVisible(false))
                next = null;

            bool enter = enterChildren;
            if (copy.NextVisible(enter))
            {
                do
                {
                    if (SerializedProperty.EqualContents(copy, next))
                        yield break;
                    yield return copy.Copy();
                }
                while (copy.NextVisible(false));
            }
        }

        public static void SerializedPropertyToObject(this SerializedProperty prop, object value)
        {
            if (value == null) return;

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: prop.intValue = System.Convert.ToInt32(value); break;
                case SerializedPropertyType.Boolean: prop.boolValue = System.Convert.ToBoolean(value); break;
                case SerializedPropertyType.Float: prop.floatValue = System.Convert.ToSingle(value); break;
                case SerializedPropertyType.String: prop.stringValue = value.ToString(); break;
                case SerializedPropertyType.Vector2:
                    if (value is List<object> v2 && v2.Count == 2)
                        prop.vector2Value = new Vector2(System.Convert.ToSingle(v2[0]), System.Convert.ToSingle(v2[1]));
                    break;
                case SerializedPropertyType.Vector3:
                    if (value is List<object> v3 && v3.Count == 3)
                        prop.vector3Value = new Vector3(System.Convert.ToSingle(v3[0]), System.Convert.ToSingle(v3[1]), System.Convert.ToSingle(v3[2]));
                    break;
                case SerializedPropertyType.Vector4:
                    if (value is List<object> v4 && v4.Count == 4)
                        prop.vector4Value = new Vector4(System.Convert.ToSingle(v4[0]), System.Convert.ToSingle(v4[1]), System.Convert.ToSingle(v4[2]), System.Convert.ToSingle(v4[3]));
                    break;
                case SerializedPropertyType.Rect:
                    if (value is List<object> r && r.Count == 4)
                        prop.rectValue = new Rect(System.Convert.ToSingle(r[0]), System.Convert.ToSingle(r[1]), System.Convert.ToSingle(r[2]), System.Convert.ToSingle(r[3]));
                    break;
                case SerializedPropertyType.Bounds:
                    if (value is List<object> b && b.Count == 6)
                        prop.boundsValue = new Bounds(
                            new Vector3(System.Convert.ToSingle(b[0]), System.Convert.ToSingle(b[1]), System.Convert.ToSingle(b[2])),
                            new Vector3(System.Convert.ToSingle(b[3]), System.Convert.ToSingle(b[4]), System.Convert.ToSingle(b[5]))
                        );
                    break;
                case SerializedPropertyType.Quaternion:
                    if (value is List<object> q && q.Count == 4)
                        prop.quaternionValue = new Quaternion(System.Convert.ToSingle(q[0]), System.Convert.ToSingle(q[1]), System.Convert.ToSingle(q[2]), System.Convert.ToSingle(q[3]));
                    break;
                case SerializedPropertyType.ObjectReference:
                    if (value is UnityEngine.Object obj) prop.objectReferenceValue = obj;
                    break;
                case SerializedPropertyType.ArraySize:
                    if (value is List<object> list)
                    {
                        prop.arraySize = list.Count;
                        for (int i = 0; i < list.Count; i++)
                            SerializedPropertyToObject(prop.GetArrayElementAtIndex(i), list[i]);
                    }
                    break;
                case SerializedPropertyType.Generic:
                    if (value is Dictionary<string, object> dict)
                    {
                        foreach (var child in GetChildren(prop, true))
                        {
                            if (dict.TryGetValue(child.name, out var childValue))
                                child.SerializedPropertyToObject(childValue);
                        }
                    }
                    break;
            }
        }

        public static Type GetSerializedObjectType(this SerializedProperty property)
        {
            if (property == null) return null;

            Type parentType = property.serializedObject.targetObject.GetType();
            string[] path = property.propertyPath.Split('.');
            FieldInfo field;

            Type currentType = parentType;
            for (int i = 0; i < path.Length; i++)
            {
                string fieldName = path[i];

                // 如果是数组或者List的元素，跳过
                if (fieldName == "Array" || fieldName == "data[0]") continue;

                field = currentType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    return null;
                }
                currentType = field.FieldType;

                // 如果是数组或List，取元素类型
                if (currentType.IsArray)
                {
                    currentType = currentType.GetElementType();
                }
                else if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                {
                    currentType = currentType.GetGenericArguments()[0];
                }
            }

            return currentType;
        }


        public static object GetSerializedObject(this SerializedProperty property)
        {
            if (property == null) return null;

            object obj = property.serializedObject.targetObject;
            string[] elements = property.propertyPath.Replace(".Array.data[", "[").Split('.');

            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    // 处理数组/列表
                    string elementName = element[..element.IndexOf("[")];
                    int index = Convert.ToInt32(element[element.IndexOf("[")..].Trim('[', ']'));

                    obj = GetFieldValue(obj, elementName);
                    if (obj is System.Collections.IEnumerable enumerable)
                    {
                        var enumerator = enumerable.GetEnumerator();
                        for (int i = 0; i <= index; i++) enumerator.MoveNext();
                        obj = enumerator.Current;
                    }
                }
                else
                {
                    obj = GetFieldValue(obj, element);
                }
            }

            return obj;
        }


        private static object GetFieldValue(object source, string fieldName)
        {
            if (source == null) return null;
            Type type = source.GetType();

            FieldInfo f = null;
            while (f == null && type != null)
            {
                f = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                type = type.BaseType;
            }
            return f?.GetValue(source);
        }

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