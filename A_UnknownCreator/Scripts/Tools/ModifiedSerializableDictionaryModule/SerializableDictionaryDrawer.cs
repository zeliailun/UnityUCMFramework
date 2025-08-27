#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
    public class SerializableDictionaryUIToolkitDrawer : PropertyDrawer
    {
        private readonly Color color1 = new Color(0, 0, 0, 0.2f);
        private readonly Color color2 = new Color(0, 0, 0, .6f);
        private readonly Color duplicateColor = new Color(1f, 0f, 0f, 0.2f); // 重复时的红色背景
        private int selectedIndex = -1;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var foldout = new Foldout
            {
                text = property.displayName,
                value = property.isExpanded,

            };

            var dictionaryListProp = property.FindPropertyRelative("dictionaryList");
            var listContainer = new VisualElement
            {
                style = {

                    flexDirection = FlexDirection.Column,
                    backgroundColor = color1,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = color1,
                    borderBottomColor = color1,
                    borderLeftColor = color1,
                    borderRightColor = color1,

                }

            };
            foldout.Add(listContainer);

            void RefreshList()
            {
                listContainer.Clear();
                property.serializedObject.Update();

                // 保存每个 KeyField 以便检测时高亮
                List<(SerializedProperty keyProp, VisualElement keyField)> keyFields = new();

                // 绘制每个 KV
                for (int i = 0; i < dictionaryListProp.arraySize; i++)
                {
                    int index = i;
                    var element = dictionaryListProp.GetArrayElementAtIndex(i);

                    var kvpContainer = new VisualElement
                    {
                        style =
                        {
                            alignContent=Align.FlexStart,
                            alignSelf = Align.Auto,
                            marginTop = 4,
                            marginBottom = 4,
                            marginLeft = 4,
                            marginRight = 4,
                            paddingBottom=2,
                            paddingTop = 2,
                            //paddingLeft = 4,
                            paddingRight = 4,
                            borderTopWidth = 1,
                            borderBottomWidth = 1,
                            borderLeftWidth = 1,
                            borderRightWidth = 1,
                            borderTopColor = color1,
                            borderBottomColor = color1,
                            borderLeftColor = color1,
                            borderRightColor = color1,
                        }
                    };
                    // 选中逻辑
                    kvpContainer.RegisterCallback<ClickEvent>(_ =>
                    {
                        selectedIndex = index;
                        // 给选中的高亮一下
                        foreach (var child in listContainer.Children())
                            child.style.backgroundColor = color1; // reset
                        kvpContainer.style.backgroundColor = new Color(0.2f, 0.4f, 1f, 0.3f); // 蓝色高亮
                    });

                    var keyProp = element.FindPropertyRelative("Key");
                    var keyField = new PropertyField(keyProp, "");
                    keyField.Bind(keyProp.serializedObject);
                    keyFields.Add((keyProp, keyField));
                    keyField.RegisterValueChangeCallback(evt =>
                    {
                        property.serializedObject.ApplyModifiedProperties();
                        HighlightDuplicates(keyFields);
                    });

                    var valueProp = element.FindPropertyRelative("Value");
                    var valueField = new PropertyField(valueProp, "");
                    valueField.Bind(valueProp.serializedObject);

                    if (valueProp.propertyType == SerializedPropertyType.Generic)
                    {
                        kvpContainer.style.flexDirection = FlexDirection.Column;
                        keyField.style.minHeight = 20;
                        valueField.style.marginLeft = !valueProp.isArray ? 14 : 3;
                    }
                    else
                    {
                        kvpContainer.style.flexDirection = FlexDirection.Row;
                        keyField.style.flexGrow = 1;
                        valueField.style.flexGrow = 1;
                    }



                    kvpContainer.Add(keyField);
                    kvpContainer.Add(valueField);
                    listContainer.Add(kvpContainer);
                }

                // Add / Remove 按钮统一放在下面
                var buttonContainer = new VisualElement
                {
                    style =
                {
                    flexDirection = FlexDirection.Row,
                    //alignContent=Align.FlexEnd,
                    alignSelf=Align.FlexEnd,
                    marginTop = 4,
                    marginBottom = 4,
                    marginLeft = 4,
                    marginRight = 4,
                    paddingLeft = 2,
                    paddingRight =2,
                    paddingTop = 2,
                    paddingBottom = 2
                }
                };

                var addButton = new Button(() =>
                {
                    property.serializedObject.Update();

                    HashSet<object> keys = new();
                    bool hasDuplicate = false;

                    for (int i = 0; i < dictionaryListProp.arraySize; i++)
                    {
                        var elementProp = dictionaryListProp.GetArrayElementAtIndex(i);
                        var keyProp = elementProp.FindPropertyRelative("Key");
                        object keyObj = keyProp.GetSerializedObject();

                        if (keyObj != null && !keys.Add(keyObj))
                        {
                            hasDuplicate = true;
                            break;
                        }
                    }

                    if (hasDuplicate)
                    {
                        EditorUtility.DisplayDialog("警告", "当前字典中已有重复 Key，请修改后再添加！", "OK");
                        property.serializedObject.ApplyModifiedProperties();
                        return;
                    }

                    dictionaryListProp.arraySize++;
                    property.serializedObject.ApplyModifiedProperties();
                    RefreshList();
                })
                {
                    text = "+",
                    style =
                    {
                        fontSize = 20,
                        marginRight = 4,
                        width = 50,
                        height= 30,
                        borderTopWidth = 1,
                        borderBottomWidth = 1,
                        borderLeftWidth = 1,
                        borderRightWidth = 1,
                        borderTopColor = color2,
                        borderBottomColor = color2,
                        borderLeftColor = color2,
                        borderRightColor = color2,
                    }
                };

                var removeButton = new Button(() =>
                {
                    property.serializedObject.Update();

                    if (dictionaryListProp.arraySize > 0)
                    {
                        int targetIndex = selectedIndex >= 0 && selectedIndex < dictionaryListProp.arraySize
                            ? selectedIndex
                            : dictionaryListProp.arraySize - 1;

                        dictionaryListProp.DeleteArrayElementAtIndex(targetIndex);
                        selectedIndex = -1; // 删除后清空选中
                    }

                    property.serializedObject.ApplyModifiedProperties();
                    RefreshList();
                })
                {
                    text = "-",
                    style =
                            {
                                fontSize = 20,
                                width = 50,
                                height= 30,
                                borderTopWidth = 1,
                                borderBottomWidth = 1,
                                borderLeftWidth = 1,
                                borderRightWidth = 1,
                                borderTopColor = color2,
                                borderBottomColor = color2,
                                borderLeftColor = color2,
                                borderRightColor = color2,
                            }
                };

                buttonContainer.Add(addButton);
                buttonContainer.Add(removeButton);
                listContainer.Add(buttonContainer);

                property.serializedObject.ApplyModifiedProperties();

                // 初始刷新时检测一次
                HighlightDuplicates(keyFields);
            }

            RefreshList();

            Undo.undoRedoPerformed += () =>
            {

                property?.serializedObject?.Update();
                RefreshList();
            };


            return foldout;
        }

        private void HighlightDuplicates(List<(SerializedProperty keyProp, VisualElement keyField)> keyFields)
        {
            HashSet<object> seen = new();
            HashSet<object> duplicates = new();

            // 找到重复项
            foreach (var (keyProp, _) in keyFields)
            {
                object val = keyProp.GetSerializedObject();
                if (val != null && !seen.Add(val))
                    duplicates.Add(val);
            }

            // 设置样式
            foreach (var (keyProp, keyField) in keyFields)
            {
                object val = keyProp.GetSerializedObject();
                if (val != null && duplicates.Contains(val))
                {
                    keyField.style.backgroundColor = duplicateColor;
                }
                else
                {
                    keyField.style.backgroundColor = Color.clear;
                }
            }
        }



    }

    //IMGUI
    /*
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
    public class SerializableDictionaryDrawer : PropertyDrawer
    {
        private ReorderableList reorderableList;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 折叠列表
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                label,
                true
            );

            if (!property.isExpanded) return;

            EditorGUI.indentLevel++;

            if (reorderableList == null)
                SetupReorderableList(property);

            reorderableList.DoList(new Rect(
                position.x,
                position.y + EditorGUIUtility.singleLineHeight + 2,
                position.width,
                reorderableList.GetHeight()
            ));

            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            if (reorderableList == null)
                SetupReorderableList(property);

            // 折叠标题 + 列表高度
            return EditorGUIUtility.singleLineHeight + 2 + reorderableList.GetHeight();
        }

        private void SetupReorderableList(SerializedProperty property)
        {
            var listProp = property.FindPropertyRelative("dictionaryList");

            reorderableList = new ReorderableList(property.serializedObject, listProp, true, false, true, true);

            reorderableList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Dictionary Entries");
            };

            reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = listProp.GetArrayElementAtIndex(index);
                var keyProp = element.FindPropertyRelative("Key");
                var valueProp = element.FindPropertyRelative("Value");
                var isDuplicateProp = element.FindPropertyRelative("isKeyDuplicated");

                // 计算 Key 高度
                float keyHeight = EditorGUI.GetPropertyHeight(keyProp);
                float valueHeight = EditorGUI.GetPropertyHeight(valueProp);

                float keyWidth = rect.width * 0.4f;
                float valueWidth = rect.width * 0.56f;

                Rect keyRect = new Rect(rect.x, rect.y, keyWidth, keyHeight);
                EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none, true);

                Rect valueRect = new Rect(rect.x + keyWidth + 20, rect.y, valueWidth, valueHeight);
                EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none, true);

                // 检查重复 Key
                HashSet<object> keys = new();
                bool isDuplicate = false;
                for (int i = 0; i < listProp.arraySize; i++)
                {
                    var k = listProp.GetArrayElementAtIndex(i).FindPropertyRelative("Key").GetSerializedObject();

                    if (!keys.Add(k) && i == index)
                        isDuplicate = true;
                }
                isDuplicateProp.boolValue = isDuplicate;

                // 重复 Key 提示
                if (isDuplicate)
                {
                    Rect errorRect = new Rect(rect.x, rect.y + Mathf.Max(keyHeight, valueHeight) + 2, rect.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.HelpBox(errorRect, "重复的 Key！", MessageType.Error);
                }
            };

            // 每行高度动态计算
            reorderableList.elementHeightCallback = index =>
            {
                var element = listProp.GetArrayElementAtIndex(index);
                var keyProp = element.FindPropertyRelative("Key");
                var valueProp = element.FindPropertyRelative("Value");
                var isDuplicateProp = element.FindPropertyRelative("isKeyDuplicated");

                float keyHeight = EditorGUI.GetPropertyHeight(keyProp, true);
                float valueHeight = EditorGUI.GetPropertyHeight(valueProp, true);

                float height = keyHeight + valueHeight + 4; // Key+Value+间距
                if (isDuplicateProp.boolValue)
                    height += EditorGUIUtility.singleLineHeight + 2; // 重复提示行

                return height;
            };
        }
    }

*/


    //Old
    /*
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
                    if (!property.isExpanded)
                        return;

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


           private void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
           {
               var kvpProp = dictionaryList.GetArrayElementAtIndex(index);
               var keyProp = kvpProp.FindPropertyRelative("Key");
               var valueProp = kvpProp.FindPropertyRelative("Value");

               Event evt = Event.current;

               // ----------------- 右键菜单 -----------------
               if (evt != null && evt.type == EventType.ContextClick && rect.Contains(evt.mousePosition))
               {
                   GenericMenu menu = new GenericMenu();
                   menu.AddItem(new GUIContent("删除"), false, () =>
                   {
                       dictionaryList.DeleteArrayElementAtIndex(index);
                       property.serializedObject.ApplyModifiedProperties();
                   });
                   menu.ShowAsContext();
                   evt.Use();
               }

               // ----------------- 计算 Rect -----------------
               float dividerPos = dividerPosProp != null ? dividerPosProp.floatValue : 0.25f;
               float dividerWidth = IsSingleLine(valueProp) ? 6f : 16f;

               Rect fullRect = rect;
               fullRect.width -= 1;
               fullRect.height -= 2;

               // ----------------- 绘制 Key -----------------
               Rect keyRect = fullRect;
               keyRect.width *= dividerPos;
               keyRect.width -= dividerWidth / 2;
               float lineHeight = EditorGUIUtility.singleLineHeight;
               Rect keyRectFixed = new Rect(keyRect.x, keyRect.y, keyRect.width, lineHeight);
               EditorGUI.PropertyField(keyRectFixed, keyProp, GUIContent.none);

               if (kvpProp.FindPropertyRelative("isKeyDuplicated").boolValue)
               {
                   GUI.Label(
                       new Rect(keyRectFixed.x + keyRectFixed.width - 20, keyRectFixed.y - 1, 20, 20),
                       EditorGUIUtility.IconContent("console.erroricon")
                   );
               }

               // ----------------- 绘制 Value -----------------

               Rect valueRect = fullRect;
               valueRect.x += fullRect.width * dividerPos;
               valueRect.width *= (1 - dividerPos);
               valueRect.width -= dividerWidth / 2;
               Rect valueDrawRect = valueRect;

               bool isArray = valueProp.isArray && valueProp.propertyType != SerializedPropertyType.String;
               bool isCustomClass = valueProp.propertyType == SerializedPropertyType.Generic && valueProp.hasVisibleChildren;

               if (isArray || isCustomClass)
               {
                   if (valueProp.isArray)
                   {
                       ReorderableList tempList = new ReorderableList(valueProp.serializedObject, valueProp, true, false, true, true);
                       tempList.drawElementCallback = (r, i, a, f) =>
                       {
                           var element = valueProp.GetArrayElementAtIndex(i);
                           r.x += 12;
                           r.width -= 12;
                           EditorGUI.PropertyField(r, element, true);
                       };
                       tempList.elementHeightCallback = i =>
                       {
                           var element = valueProp.GetArrayElementAtIndex(i);
                           return EditorGUI.GetPropertyHeight(element, true) + 2;
                       };
                       tempList.DoList(valueDrawRect);
                   }
                   else
                   {
                       valueDrawRect.x += 10;
                       EditorGUI.PropertyField(valueDrawRect, valueProp, true);
                   }
               }
               else
               {
                   valueDrawRect.y -= 2;
                   EditorGUI.PropertyField(valueDrawRect, valueProp, GUIContent.none);
               }

               // ----------------- 绘制 Divider -----------------
               Rect dividerRect = fullRect;
               dividerRect.x += fullRect.width * dividerPos - dividerWidth / 2;
               dividerRect.width = dividerWidth;
               Rect effectiveDividerRect = new Rect(dividerRect.x + 10, dividerRect.y, dividerRect.width - 10, dividerRect.height);
               EditorGUIUtility.AddCursorRect(effectiveDividerRect, MouseCursor.ResizeHorizontal);

               if (dividerRect.Contains(evt.mousePosition))
               {
                   if (evt.type == EventType.MouseDown)
                       isDividerDragged = true;
                   else if (evt.type == EventType.MouseUp || evt.type == EventType.MouseMove || evt.type == EventType.MouseLeaveWindow)
                       isDividerDragged = false;
               }

               if (isDividerDragged && dividerPosProp != null && evt.type == EventType.MouseDrag)
               {
                   dividerPosProp.floatValue = Mathf.Clamp(dividerPosProp.floatValue + evt.delta.x / rect.width, 0.2f, 0.8f);
               }
           }


           private float GetListElementHeight(int index)
           {
               var kvpProp = dictionaryList.GetArrayElementAtIndex(index);
               var valueProp = kvpProp.FindPropertyRelative("Value");

               float keyHeight = EditorGUIUtility.singleLineHeight; // Key 输入框高度固定
               float valueHeight;

               if (valueProp.propertyType == SerializedPropertyType.Generic && valueProp.isArray)
               {
                   if (!valueProp.isExpanded)
                       valueHeight = 0;
                   else
                   {
                       ReorderableList tempList = new ReorderableList(valueProp.serializedObject, valueProp, true, false, true, true);
                       tempList.elementHeightCallback = i =>
                       {
                           var element = valueProp.GetArrayElementAtIndex(i);
                           return EditorGUI.GetPropertyHeight(element, true) + 2;
                       };
                       valueHeight = tempList.GetHeight();
                   }
               }
               else
               {
                   if (valueProp.isExpanded && !IsSingleLine(valueProp))
                   {
                       valueHeight = EditorGUI.GetPropertyHeight(valueProp, true) - EditorGUIUtility.singleLineHeight;
                   }
                   else
                   {
                       valueHeight = 0;
                   }
               }

               return keyHeight + valueHeight + 4; // 4 = 行间小间距
           }


           private void ShowDictIsEmptyMessage(Rect rect)
           {
               GUI.Label(rect, "Empty");
           }


           private bool IsSingleLine(SerializedProperty prop)
           {
               return prop != null && (prop.propertyType != SerializedPropertyType.Generic || prop.hasVisibleChildren == false);
           }

       }*/


}
#endif