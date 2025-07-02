#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public enum HierarchyBG
    {
        Color,
        Texture
    }

    [InitializeOnLoad]
    public class CustomHierarchy : MonoBehaviour
    {
        public Texture avatar;
        public Color fontColor = Color.white;
        public HierarchyBG backgroundType = HierarchyBG.Color;
        public Color BorderColor = Color.black;
        public Color backgroundColor = Color.white;
        public Texture backgroundTexture;
        public Color backgroundActiveColor = Color.blue;


        CustomHierarchy()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }



        private void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (this == null)
            {
                EditorApplication.hierarchyWindowItemOnGUI -= HandleHierarchyWindowItemOnGUI;
                return;
            }

            var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj != null && instanceID == gameObject.GetInstanceID())
            {
                var prefabType = PrefabUtility.GetPrefabAssetType(obj);
                if (prefabType != PrefabAssetType.MissingAsset)
                {
                    if (backgroundType == HierarchyBG.Color)
                    {
                        EditorGUI.DrawRect(selectionRect, backgroundColor);
                        DrawBorderRect(selectionRect, BorderColor, 1);
                    }
                    else
                    {
                        if (backgroundTexture != null)
                            EditorGUI.DrawPreviewTexture(selectionRect, backgroundTexture);
                    }


                    if (Selection.instanceIDs.Contains(instanceID))
                    {
                        EditorGUI.DrawRect(selectionRect, backgroundActiveColor);
                        DrawBorderRect(selectionRect, BorderColor, 1);
                    }


                    var title = new GUIStyle();
                    title.normal.textColor = fontColor;
                    title.fontStyle = FontStyle.Bold;
                    title.alignment = TextAnchor.MiddleCenter;
                    EditorGUI.DropShadowLabel(selectionRect, obj.name, title);
                    if (avatar != null)
                    {
                        GUIContent icon = new(avatar);
                        Rect rect = new(selectionRect.x, selectionRect.y, 18, selectionRect.height);
                        EditorGUI.LabelField(rect, icon);
                    }

                }
            }
        }


        private void DrawBorderRect(Rect area, Color color, float borderWidth)
        {
            //------------------------------------------------
            float x1 = area.x;
            float y1 = area.y;
            float x2 = area.width;
            float y2 = borderWidth;

            Rect lineRect = new Rect(x1, y1, x2, y2);

            EditorGUI.DrawRect(lineRect, color);

            //------------------------------------------------
            x1 = area.x + area.width;
            y1 = area.y;
            x2 = borderWidth;
            y2 = area.height;

            lineRect = new Rect(x1, y1, x2, y2);

            EditorGUI.DrawRect(lineRect, color);

            //------------------------------------------------
            x1 = area.x;
            y1 = area.y;
            x2 = borderWidth;
            y2 = area.height;

            lineRect = new Rect(x1, y1, x2, y2);

            EditorGUI.DrawRect(lineRect, color);

            //------------------------------------------------
            x1 = area.x;
            y1 = area.y + area.height;
            x2 = area.width;
            y2 = borderWidth;

            lineRect = new Rect(x1, y1, x2, y2);

            EditorGUI.DrawRect(lineRect, color);
        }

    }
}
#endif