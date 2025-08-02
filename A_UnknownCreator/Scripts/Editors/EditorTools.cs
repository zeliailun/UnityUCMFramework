#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnknownCreator.Modules;

internal static class EditorTools
{
    [MenuItem("GameObject/UnknownCreator/EmptyUnit", false, 0)]
    public static void CreatePlayer()
    {
        var unit = new GameObject("Unit");
        var model = new GameObject(UnitGlobals.Model);
        model.SetLayer(2);
        model.transform.SetParent(unit.transform);
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


    [MenuItem("GameObject/UnknownCreator/CopyPath %Q")]
    public static void CopyPath()
    {
        var obj = GetTarget<GameObject>();
        if (obj == null) return;

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

    private static T GetTarget<T>() where T : Object
    {
        Object[] objs = Selection.objects;
        if (objs.Length < 1)
            return default;

        T obj = objs[0] as T;
        if (obj == null)
            return default;

        return obj;
    }

}
#endif