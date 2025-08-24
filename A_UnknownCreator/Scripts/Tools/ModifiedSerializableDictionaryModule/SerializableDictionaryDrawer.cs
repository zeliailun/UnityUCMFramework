#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
    public class SerializableDictionaryDrawer : PropertyDrawer
    {

        private ReorderableList reorderableList;
        private SerializedProperty property;
        private SerializedProperty dictionaryList;
        private SerializedProperty dividerPosProp;
        private bool isDividerDragged;

        public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
        {
            var indentedRect = EditorGUI.IndentedRect(rect);
            var headerRect = indentedRect;
            headerRect.height = EditorGUIUtility.singleLineHeight;

            SetupProps(prop);

            void Header()
            {
                var fullHeaderRect = new Rect(headerRect);
                //fullHeaderRect.x -= 17;
                //fullHeaderRect.width += 34;

                // 绘制背景高亮
                if (Event.current != null && fullHeaderRect.Contains(Event.current.mousePosition))
                {
                    Color transparentGrey = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                    EditorGUI.DrawRect(fullHeaderRect, transparentGrey);
                }

                // 右键菜单
                Event e = Event.current;
                if (headerRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        prop.isExpanded = !prop.isExpanded;
                        e.Use();
                    }

                    if (e.type == EventType.ContextClick)
                    {
                        GenericMenu menu = new();

                        menu.AddItem(new GUIContent("复制字典"), false, () =>
                        {
                            var jsonDictList = new List<Dictionary<string, object>>();
                            for (int i = 0; i < dictionaryList.arraySize; i++)
                            {
                                var kvpProp = dictionaryList.GetArrayElementAtIndex(i);
                                var key = kvpProp.FindPropertyRelative("Key").GetSerializedObject();
                                var value = kvpProp.FindPropertyRelative("Value").GetSerializedObject();
                                jsonDictList.Add(new Dictionary<string, object>
                                {
                                    ["Key"] = key,
                                    ["Value"] = value
                                });
                            }

                            string json = JsonMapper.ToJson(jsonDictList);
                            GUIUtility.systemCopyBuffer = json;
                        });


                        if (CanPasteDictionaryFromClipboard(out var dictList))
                        {
                            menu.AddItem(new GUIContent("粘贴字典"), false, () =>
                            {
                                dictionaryList.ClearArray();
                                foreach (var kvp in dictList)
                                {
                                    int newIndex = dictionaryList.arraySize;
                                    dictionaryList.arraySize++;
                                    var kvpProp = dictionaryList.GetArrayElementAtIndex(newIndex);

                                    kvpProp.FindPropertyRelative("Key").SerializedPropertyToObject(kvp["Key"]);
                                    kvpProp.FindPropertyRelative("Value").SerializedPropertyToObject(kvp["Value"]);
                                }
                                dictionaryList.serializedObject.ApplyModifiedProperties();
                            });
                        }
                        else
                        {
                            menu.AddDisabledItem(new GUIContent("粘贴字典"));
                        }


                        if (CanPasteFromClipboard(out var dict))
                        {
                            menu.AddItem(new GUIContent("粘贴新项目"), false, () =>
                            {
                                int newIndex = dictionaryList.arraySize;
                                dictionaryList.arraySize++;
                                var kvpProp = dictionaryList.GetArrayElementAtIndex(newIndex);
                                kvpProp.FindPropertyRelative("Key").SerializedPropertyToObject(dict["Key"]);
                                kvpProp.FindPropertyRelative("Value").SerializedPropertyToObject(dict["Value"]);

                                dictionaryList.serializedObject.ApplyModifiedProperties();
                            });
                        }
                        else
                        {
                            menu.AddDisabledItem(new GUIContent("粘贴新项目"));
                        }

                        menu.AddItem(new GUIContent("清空字典"), false, () =>
                        {
                            dictionaryList.ClearArray();
                            dictionaryList.serializedObject.ApplyModifiedProperties();
                        });

                        menu.ShowAsContext();
                        e.Use();
                    }
                }

                // 绘制折叠三角
                EditorGUI.Foldout(fullHeaderRect, property.isExpanded, "");

                //名称
                GUI.Label(headerRect, prop.displayName);
                GUI.color = Color.white;
                GUI.skin.label.fontSize = 12;
                GUI.skin.label.fontStyle = FontStyle.Normal;
                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            }

            void KeysWarning()
            {
                if (Event.current != null && Event.current.type != EventType.Repaint)
                {
                    return;
                }

                var hasRepeated = false;
                var repeatedKeys = new List<string>();

                for (int i = 0; i < dictionaryList.arraySize; i++)
                {
                    SerializedProperty isKeyRepeatedProperty = dictionaryList.GetArrayElementAtIndex(i)
                                                                       .FindPropertyRelative("isKeyDuplicated");

                    if (isKeyRepeatedProperty.boolValue)
                    {
                        hasRepeated = true;
                        SerializedProperty keyProperty = dictionaryList.GetArrayElementAtIndex(i).FindPropertyRelative("Key");
                        string keyString = keyProperty.propertyType switch
                        {
                            SerializedPropertyType.Integer => keyProperty.intValue.ToString(),
                            SerializedPropertyType.Boolean => keyProperty.boolValue.ToString(),
                            SerializedPropertyType.Float => keyProperty.floatValue.ToString(),
                            SerializedPropertyType.String => keyProperty.stringValue,
                            SerializedPropertyType.Enum => keyProperty.enumDisplayNames.Length > 0
                                                                        ? keyProperty.enumDisplayNames[keyProperty.enumValueIndex]
                                                                        : keyProperty.enumValueIndex.ToString(),
                            SerializedPropertyType.ObjectReference => keyProperty.objectReferenceValue != null
                                                                        ? keyProperty.objectReferenceValue.name
                                                                        : "null",
                            SerializedPropertyType.Color => keyProperty.colorValue.ToString(),
                            SerializedPropertyType.Vector2 => keyProperty.vector2Value.ToString(),
                            SerializedPropertyType.Vector3 => keyProperty.vector3Value.ToString(),
                            SerializedPropertyType.Vector4 => keyProperty.vector4Value.ToString(),
                            SerializedPropertyType.Rect => keyProperty.rectValue.ToString(),
                            SerializedPropertyType.Quaternion => keyProperty.quaternionValue.eulerAngles.ToString(),
                            SerializedPropertyType.Character => ((char)keyProperty.intValue).ToString(),
                            SerializedPropertyType.Bounds => keyProperty.boundsValue.ToString(),
                            SerializedPropertyType.ManagedReference => keyProperty.GetSerializedObjectType().Name,
                            _ => "(其它类型)"
                        };
                        repeatedKeys.Add(keyString);
                    }
                }

                if (!hasRepeated)
                {
                    return;
                }

                float with = GUI.skin.label.CalcSize(new GUIContent(prop.displayName)).x;
                headerRect.x += with + 30f;
                var warningRect = headerRect;
                Rect warningRectIcon = new(headerRect.x - 18, headerRect.y, headerRect.width, headerRect.height);
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUI.skin.label.fontStyle = FontStyle.Bold;
                GUI.Label(warningRect, "重复keys: " + string.Join(", ", repeatedKeys));
            }

            void List()
            {
                if (!prop.isExpanded)
                {
                    return;
                }

                SetupList(prop);

                float newHeight = indentedRect.height - EditorGUIUtility.singleLineHeight - 3;
                indentedRect.y += indentedRect.height - newHeight;
                indentedRect.height = newHeight;

                reorderableList.DoList(indentedRect);
            }

            Header();
            KeysWarning();
            List();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            SetupProps(prop);

            var height = EditorGUIUtility.singleLineHeight;

            if (prop.isExpanded)
            {
                SetupList(prop);
                height += reorderableList.GetHeight() + 5;
            }

            return height;
        }

        private void SetupList(SerializedProperty prop)
        {
            if (reorderableList != null)
            {
                return;
            }

            SetupProps(prop);

            this.reorderableList = new ReorderableList(dictionaryList.serializedObject, dictionaryList, true, false, true, true)
            {
                drawElementCallback = DrawListElement,
                elementHeightCallback = GetListElementHeight,
                drawNoneElementCallback = ShowDictIsEmptyMessage
            };
        }

        public void SetupProps(SerializedProperty prop)
        {
            if (this.property != null)
            {
                return;
            }

            this.property = prop;
            this.dictionaryList = prop.FindPropertyRelative("dictionaryList");
            this.dividerPosProp = prop.FindPropertyRelative("dividerPos");
        }

        private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            Rect keyRect;
            Rect valueRect;
            Rect dividerRect;

            var kvpProp = dictionaryList.GetArrayElementAtIndex(index);
            var keyProp = kvpProp.FindPropertyRelative("Key");
            var valueProp = kvpProp.FindPropertyRelative("Value");


            void ItemMenu()
            {
                Event e = Event.current;
                if (e != null && e.type == EventType.ContextClick && rect.Contains(e.mousePosition))
                {
                    GenericMenu menu = new GenericMenu();

                    menu.AddItem(new GUIContent("复制"), false, () =>
                    {
                        var kvpProp = dictionaryList.GetArrayElementAtIndex(index);
                        var jsonDict = new Dictionary<string, object>
                        {
                            ["Key"] = keyProp.GetSerializedObject(),
                            ["Value"] = valueProp.GetSerializedObject(),
                        };
                        string json = JsonMapper.ToJson(jsonDict);
                        GUIUtility.systemCopyBuffer = json;
                    });

                    if (CanPasteFromClipboard(out var dict1))
                    {
                        menu.AddItem(new GUIContent("覆盖"), false, () =>
                        {
                            var kvpProp = dictionaryList.GetArrayElementAtIndex(index);
                            kvpProp.FindPropertyRelative("Key").SerializedPropertyToObject(dict1["Key"]);
                            kvpProp.FindPropertyRelative("Value").SerializedPropertyToObject(dict1["Value"]);
                            property.serializedObject.ApplyModifiedProperties();
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("覆盖"));
                    }


                    if (CanPasteFromClipboard(out var dict2))
                    {
                        menu.AddItem(new GUIContent("粘贴新项目"), false, () =>
                        {
                            int newIndex = dictionaryList.arraySize;
                            dictionaryList.arraySize++;
                            var kvpProp = dictionaryList.GetArrayElementAtIndex(newIndex);
                            kvpProp.FindPropertyRelative("Key").SerializedPropertyToObject(dict2["Key"]);
                            kvpProp.FindPropertyRelative("Value").SerializedPropertyToObject(dict2["Value"]);
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("粘贴新项目"));
                    }


                    menu.AddItem(new GUIContent("删除"), false, () =>
                    {
                        dictionaryList.DeleteArrayElementAtIndex(index);
                        property.serializedObject.ApplyModifiedProperties();
                    });

                    menu.ShowAsContext();
                    e.Use();
                }
            }

            void Draw(Rect rect, SerializedProperty prop)
            {
                if (IsSingleLine(prop))
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(rect, prop, GUIContent.none);
                }
                else
                {
                    Rect foldoutRect = new Rect(rect.x + 12, rect.y, rect.width - 12, EditorGUIUtility.singleLineHeight);
                    prop.isExpanded = EditorGUI.Foldout(foldoutRect, prop.isExpanded, prop.GetSerializedObject().GetType().Name, false);

                    rect.y += EditorGUIUtility.singleLineHeight + 2;
                    if (prop.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var childProp in GetChildren(prop, false))
                        {
                            float childHeight = EditorGUI.GetPropertyHeight(childProp, true);
                            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, childHeight), childProp, true);
                            rect.y += childHeight + 2;
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }

            void DrawRects()
            {
                var dividerWidh = IsSingleLine(valueProp) ? 6 : 16f;
                var dividerPosition = 0.25f;

                var fullRect = rect;
                fullRect.width -= 1;
                fullRect.height -= 2;

                keyRect = fullRect;
                keyRect.width *= dividerPosition;
                keyRect.width -= dividerWidh / 2;

                valueRect = fullRect;
                valueRect.x += fullRect.width * dividerPosition;
                valueRect.width *= (1 - dividerPosition);
                valueRect.width -= dividerWidh / 2;

                dividerRect = fullRect;
                dividerRect.x += fullRect.width * dividerPosition - dividerWidh / 2;
                dividerRect.width = dividerWidh;
            }

            void Key()
            {
                Draw(keyRect, keyProp);

                if (kvpProp.FindPropertyRelative("isKeyDuplicated").boolValue)
                {
                    GUI.Label(new Rect(keyRect.x + keyRect.width - 20, keyRect.y - 1, 20, 20),
                              EditorGUIUtility.IconContent("console.erroricon"));
                }
            }

            void Value()
            {
                Draw(valueRect, valueProp);

#if !ODIN_INSPECTOR
                if (valueProp.type.StartsWith("InterfaceHolder"))
                {
                    var interfaceValue = valueProp.FindPropertyRelative("value");
                    MonoBehaviour newValue = (MonoBehaviour)EditorGUI.ObjectField(valueRect,
                                              interfaceValue.objectReferenceValue, typeof(MonoBehaviour), true);

                    if (interfaceValue.objectReferenceValue != newValue)
                    {
                        if (newValue == null || newValue.GetComponent(
                            fieldInfo.FieldType.GenericTypeArguments[1].GenericTypeArguments[0]) != null)
                        {
                            interfaceValue.objectReferenceValue = newValue;
                        }
                        else
                        {
                            Debug.LogWarning($"Assigned object must implement interface " +
                                             $"{fieldInfo.FieldType.GenericTypeArguments[1].GenericTypeArguments[0].Name}");
                        }
                    }
                }
#endif
            }

            void Divider()
            {
                EditorGUIUtility.AddCursorRect(dividerRect, MouseCursor.ResizeHorizontal);

                if (Event.current == null || rect.Contains(Event.current.mousePosition) == false)
                {
                    return;
                }

                if (Event.current != null && dividerRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.MouseDown)
                    {
                        isDividerDragged = true;
                    }
                    else if (Event.current.type == EventType.MouseUp
                             || Event.current.type == EventType.MouseMove
                             || Event.current.type == EventType.MouseLeaveWindow)
                    {
                        isDividerDragged = false;
                    }
                }

                if (isDividerDragged && dividerPosProp != null && Event.current != null && Event.current.type == EventType.MouseDrag)
                {
                    dividerPosProp.floatValue = Mathf.Clamp(dividerPosProp.floatValue + Event.current.delta.x / rect.width, .2f, .8f);
                }
            }


            ItemMenu();
            DrawRects();
            Key();
            Value();
            Divider();
        }

        private float GetListElementHeight(int index)
        {
            var kvpProp = dictionaryList.GetArrayElementAtIndex(index);
            var keyProp = kvpProp.FindPropertyRelative("Key");
            var valueProp = kvpProp.FindPropertyRelative("Value");

            float GetPropertyHeight(SerializedProperty prop)
            {
                if (IsSingleLine(prop))
                    return EditorGUIUtility.singleLineHeight;

                float height = EditorGUIUtility.singleLineHeight; // Foldout 本身高度

                if (prop.isExpanded)
                {
                    foreach (var child in GetChildren(prop, false))
                    {
                        height += EditorGUI.GetPropertyHeight(child, true) + 2;
                    }
                }

                return height;
            }

            return Mathf.Max(GetPropertyHeight(keyProp), GetPropertyHeight(valueProp));
        }

        private void ShowDictIsEmptyMessage(Rect rect)
        {
            GUI.Label(rect, "Empty");
        }

        private IEnumerable<SerializedProperty> GetChildren(SerializedProperty prop, bool enterVisibleGrandchildren)
        {
            if (prop == null) yield return null;

            prop = prop.Copy();

            var startPath = prop.propertyPath;

            var enterVisibleChildren = true;

            while (prop.NextVisible(enterVisibleChildren) && prop.propertyPath.StartsWith(startPath))
            {
                yield return prop;
                enterVisibleChildren = enterVisibleGrandchildren;
            }
        }

        private bool IsSingleLine(SerializedProperty prop)
        {
            return prop != null && (prop.propertyType != SerializedPropertyType.Generic || prop.hasVisibleChildren == false);
        }

        private bool CanPasteFromClipboard(out Dictionary<string, object> dict)
        {
            string json = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(json))
            {
                dict = null;
                return false;
            }

            json = json.Trim();
            if (!json.StartsWith("{") || !json.EndsWith("}"))
            {
                dict = null;
                return false;
            }

            dict = JsonMapper.ToObject<Dictionary<string, object>>(json);
            return dict != null && dict.ContainsKey("Key") && dict.ContainsKey("Value");
        }

        private bool CanPasteDictionaryFromClipboard(out List<Dictionary<string, object>> dictList)
        {
            dictList = null;
            string json = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(json)) return false;

            json = json.Trim();
            if (!json.StartsWith("[") || !json.EndsWith("]")) return false; // 整个字典列表应该是 JSON 数组

            dictList = JsonMapper.ToObject<List<Dictionary<string, object>>>(json);
            if (dictList == null) return false;

            // 检查每个元素是否有 Key 和 Value
            foreach (var kvp in dictList)
            {
                if (!kvp.ContainsKey("Key") || !kvp.ContainsKey("Value"))
                    return false;
            }

            return true;
        }
    }
}
#endif