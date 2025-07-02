#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ObjPathCopyTool
{
    [MenuItem("GameObject/CopyPath %Q")]//自定义快捷键
    static void CopyPath()
    {
        Object[] objs = Selection.objects;
        if (objs.Length < 1)
            return;

        GameObject obj = objs[0] as GameObject;
        if (!obj)
            return;

        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent)
        {
            path = string.Format("{0}/{1}", parent.name, path);
            parent = parent.parent;
        }

        Debug.Log(path);
        CopyString(path);
    }

    //将字符串赋值到剪切板
    static void CopyString(string str)
    {
        TextEditor te = new TextEditor();
        te.text = str;
        te.SelectAll();
        te.Copy();
    }
}
#endif