#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBase), true)]
    public class MonoBaseUITKEditor : UnknownCreatorUITKEditorBase
    {
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(CustomScriptableObject), true)]
    public class CustomScriptableObjectUITKEditor : UnknownCreatorUITKEditorBase
    {
    }

    public abstract class UnknownCreatorUITKEditorBase : Editor
    {
        private const int GeneratedFieldApplyDelay = 100;
        private const int RefreshDelay = 1;

        private const string RowPrefix = "uc-property-row-";
        private const string InfoPrefix = "uc-info-added-";
        private const string OpenButtonPrefix = "uc-open-button-added-";
        private const string FoldoutStatePrefix = "UnknownCreator.Editor.Foldout.";
        private const string PersistentFoldoutClass = "uc-persistent-foldout";

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            bool hideScript = Attribute.IsDefined(
                target.GetType(),
                typeof(HideScriptFieldAttribute),
                true
            );

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                SerializedProperty property = iterator.Copy();

                if (property.name == "m_Script" && hideScript)
                    continue;

                root.Add(CreatePropertyBlock(property));
            }

            root.Bind(serializedObject);

            ApplyAttributesForGeneratedFields(root);
            RegisterAttributeRefresh(root);

            return root;
        }

        private VisualElement CreatePropertyBlock(SerializedProperty property)
        {
            VisualElement block = new VisualElement();
            block.style.marginBottom = 3;

            FieldInfo fieldInfo = GetFieldInfo(target.GetType(), property.name);
            FieldAttributes attributes = GetFieldAttributes(fieldInfo);

            if (attributes.Guid != null)
                TryInitGuid(property);

            if (attributes.Info != null && !string.IsNullOrEmpty(attributes.Info.message))
            {
                block.Add(CreateInfoBox(attributes.Info));
            }

            string labelName = attributes.DisplayName != null
                ? attributes.DisplayName.name
                : property.displayName;

            PropertyField propertyField = new PropertyField(property, labelName);
            propertyField.label = labelName;
            propertyField.style.flexGrow = 1;

            ApplyEnabledState(
                propertyField,
                property,
                attributes.ReadOnly,
                attributes.EnableIfs
            );

            bool hasOpenButton =
                attributes.OpenInspector != null &&
                property.propertyType == SerializedPropertyType.ObjectReference;

            bool hasGuidButton =
                attributes.Guid != null &&
                property.propertyType == SerializedPropertyType.String;

            if (hasOpenButton || hasGuidButton)
            {
                VisualElement row = CreatePropertyRow(propertyField, property.propertyPath);

                if (hasOpenButton)
                    row.Add(CreateOpenInspectorButton(property, propertyField));

                if (hasGuidButton)
                    row.Add(CreateRefreshGuidButton(property));

                block.Add(row);
            }
            else
            {
                block.Add(propertyField);
            }

            if (attributes.DisplayName != null)
            {
                propertyField.RegisterCallback<AttachToPanelEvent>(_ =>
                {
                    propertyField.schedule.Execute(() =>
                    {
                        ApplyDisplayName(propertyField, attributes.DisplayName);
                    }).ExecuteLater(RefreshDelay);
                });
            }

            propertyField.userData = block;

            ApplyVisibleState(
                block,
                property,
                attributes.ShowIfs,
                attributes.HideIfs
            );

            return block;
        }

        private void ApplyAttributesForGeneratedFields(VisualElement root)
        {
            root.schedule.Execute(() =>
            {
                foreach (PropertyField propertyField in root.Query<PropertyField>().ToList())
                {
                    if (string.IsNullOrEmpty(propertyField.bindingPath))
                        continue;

                    SerializedProperty property = serializedObject.FindProperty(propertyField.bindingPath);
                    if (property == null)
                        continue;

                    if (property.depth == 0)
                        continue;

                    FieldInfo fieldInfo = GetFieldInfoByPropertyPath(target.GetType(), property.propertyPath);
                    if (fieldInfo == null)
                        continue;

                    FieldAttributes attributes = GetFieldAttributes(fieldInfo);

                    if (attributes.DisplayName != null)
                        ApplyDisplayName(propertyField, attributes.DisplayName);

                    if (attributes.OpenInspector != null &&
                        property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        AddOpenInspectorButton(propertyField, property);
                    }

                    if (attributes.Info != null && !string.IsNullOrEmpty(attributes.Info.message))
                        AddInfoBoxAbovePropertyField(propertyField, attributes.Info);

                    ApplyEnabledState(
                        propertyField,
                        property,
                        attributes.ReadOnly,
                        attributes.EnableIfs
                    );

                    ApplyGeneratedVisibleState(
                        propertyField,
                        property,
                        attributes.ShowIfs,
                        attributes.HideIfs
                    );
                }

                ApplyFoldoutStates(root);
                RefreshAttributeStates(root);
            }).ExecuteLater(GeneratedFieldApplyDelay);
        }

        private void ApplyFoldoutStates(VisualElement root)
        {
            if (target == null)
                return;

            string targetTypeName = target.GetType().FullName;
            foreach (PropertyField propertyField in root.Query<PropertyField>().ToList())
            {
                if (string.IsNullOrEmpty(propertyField.bindingPath))
                    continue;

                Foldout foldout = propertyField.Q<Foldout>();
                if (foldout == null || foldout.ClassListContains(PersistentFoldoutClass))
                    continue;

                string bindingPath = propertyField.bindingPath;
                string stateKey = FoldoutStatePrefix + targetTypeName + "." + bindingPath;
                SerializedProperty property = serializedObject.FindProperty(bindingPath);
                bool isExpanded = SessionState.GetBool(stateKey, property?.isExpanded ?? foldout.value);

                foldout.SetValueWithoutNotify(isExpanded);
                if (property != null)
                    property.isExpanded = isExpanded;

                foldout.AddToClassList(PersistentFoldoutClass);
                foldout.RegisterValueChangedCallback(evt =>
                {
                    SessionState.SetBool(stateKey, evt.newValue);
                    SerializedProperty currentProperty = serializedObject.FindProperty(bindingPath);
                    if (currentProperty != null)
                        currentProperty.isExpanded = evt.newValue;
                });
            }
        }

        private void RegisterAttributeRefresh(VisualElement root)
        {
            root.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                root.schedule.Execute(() =>
                {
                    ApplyAttributesForGeneratedFields(root);
                    RefreshAttributeStates(root);
                }).ExecuteLater(RefreshDelay);
            });

            root.schedule.Execute(() =>
            {
                RefreshAttributeStates(root);
            }).ExecuteLater(GeneratedFieldApplyDelay);
        }

        private void RefreshAttributeStates(VisualElement root)
        {
            serializedObject.Update();

            foreach (PropertyField propertyField in root.Query<PropertyField>().ToList())
            {
                if (string.IsNullOrEmpty(propertyField.bindingPath))
                    continue;

                SerializedProperty property = serializedObject.FindProperty(propertyField.bindingPath);
                if (property == null)
                    continue;

                FieldInfo fieldInfo = property.depth == 0
                    ? GetFieldInfo(target.GetType(), property.name)
                    : GetFieldInfoByPropertyPath(target.GetType(), property.propertyPath);

                if (fieldInfo == null && property.propertyPath != "m_Script")
                    continue;

                FieldAttributes attributes = GetFieldAttributes(fieldInfo);

                ApplyEnabledState(
                    propertyField,
                    property,
                    attributes.ReadOnly,
                    attributes.EnableIfs
                );

                if (property.depth == 0)
                {
                    VisualElement block = propertyField.userData as VisualElement;
                    if (block != null)
                    {
                        ApplyVisibleState(
                            block,
                            property,
                            attributes.ShowIfs,
                            attributes.HideIfs
                        );
                    }
                }
                else
                {
                    ApplyGeneratedVisibleState(
                        propertyField,
                        property,
                        attributes.ShowIfs,
                        attributes.HideIfs
                    );
                }
            }
        }

        private void TryInitGuid(SerializedProperty property)
        {
            if (serializedObject.isEditingMultipleObjects)
                return;

            if (property.propertyType != SerializedPropertyType.String)
                return;

            if (!string.IsNullOrEmpty(property.stringValue))
                return;

            property.stringValue = Guid.NewGuid().ToString();
            property.serializedObject.ApplyModifiedProperties();
        }

        private VisualElement CreatePropertyRow(PropertyField propertyField, string bindingPath)
        {
            VisualElement row = new VisualElement();
            row.name = RowPrefix + MakeSafeBindingPath(bindingPath);
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            propertyField.style.flexGrow = 1;
            row.Add(propertyField);

            return row;
        }

        private HelpBox CreateInfoBox(InfoAttribute info)
        {
            HelpBox helpBox = new HelpBox(
                info.message,
                ConvertMessageType(info.type)
            );

            helpBox.style.marginBottom = 4;
            return helpBox;
        }

        private Button CreateRefreshGuidButton(SerializedProperty property)
        {
            Button button = new Button(() =>
            {
                if (serializedObject.isEditingMultipleObjects)
                    return;

                property.serializedObject.Update();

                SerializedProperty currentProperty =
                    property.serializedObject.FindProperty(property.propertyPath);

                if (currentProperty == null ||
                    currentProperty.propertyType != SerializedPropertyType.String)
                    return;

                currentProperty.stringValue = Guid.NewGuid().ToString();
                currentProperty.serializedObject.ApplyModifiedProperties();
            })
            {
                text = "刷新ID"
            };

            button.style.minWidth = 70;
            button.style.marginLeft = 4;
            button.style.flexShrink = 0;

            button.SetEnabled(
                !serializedObject.isEditingMultipleObjects &&
                property.propertyType == SerializedPropertyType.String
            );

            return button;
        }

        private Button CreateOpenInspectorButton(SerializedProperty property, PropertyField propertyField)
        {
            Button button = new Button(() =>
            {
                property.serializedObject.Update();

                SerializedProperty currentProperty =
                    property.serializedObject.FindProperty(property.propertyPath);

                if (currentProperty != null && currentProperty.objectReferenceValue != null)
                {
                    EditorUtility.OpenPropertyEditor(currentProperty.objectReferenceValue);
                }
            })
            {
                text = "编辑"
            };

            button.style.minWidth = 50;
            button.style.marginLeft = 4;
            button.style.flexShrink = 0;

            button.SetEnabled(property.objectReferenceValue != null);

            propertyField.RegisterValueChangeCallback(_ =>
            {
                property.serializedObject.Update();

                SerializedProperty currentProperty =
                    property.serializedObject.FindProperty(property.propertyPath);

                button.SetEnabled(currentProperty != null && currentProperty.objectReferenceValue != null);
            });

            return button;
        }

        private void AddOpenInspectorButton(PropertyField propertyField, SerializedProperty property)
        {
            VisualElement parent = propertyField.parent;
            if (parent == null)
                return;

            if (IsPropertyRow(parent))
                return;

            string marker = OpenButtonPrefix + MakeSafeBindingPath(propertyField.bindingPath);

            if (parent.Q<Button>(marker) != null)
                return;

            int oldIndex = parent.IndexOf(propertyField);
            if (oldIndex < 0)
                return;

            parent.Remove(propertyField);

            VisualElement row = CreatePropertyRow(propertyField, propertyField.bindingPath);

            Button button = new Button(() =>
            {
                property.serializedObject.Update();

                SerializedProperty currentProperty =
                    property.serializedObject.FindProperty(property.propertyPath);

                if (currentProperty != null && currentProperty.objectReferenceValue != null)
                {
                    EditorUtility.OpenPropertyEditor(currentProperty.objectReferenceValue);
                }
            })
            {
                name = marker,
                text = "编辑"
            };

            button.style.minWidth = 50;
            button.style.marginLeft = 4;
            button.style.flexShrink = 0;

            button.SetEnabled(property.objectReferenceValue != null);

            propertyField.RegisterValueChangeCallback(_ =>
            {
                property.serializedObject.Update();

                SerializedProperty currentProperty =
                    property.serializedObject.FindProperty(property.propertyPath);

                button.SetEnabled(currentProperty != null && currentProperty.objectReferenceValue != null);
            });

            row.Add(button);
            parent.Insert(oldIndex, row);
        }

        private void AddInfoBoxAbovePropertyField(PropertyField propertyField, InfoAttribute info)
        {
            VisualElement parent = propertyField.parent;
            if (parent == null)
                return;

            VisualElement insertTarget = propertyField;
            VisualElement searchRoot = parent;

            if (IsPropertyRow(parent))
            {
                insertTarget = parent;
                searchRoot = parent.parent;
            }

            if (searchRoot == null)
                return;

            string marker = GetInfoMarker(propertyField.bindingPath);

            if (searchRoot.Q<HelpBox>(marker) != null)
                return;

            HelpBox helpBox = new HelpBox(
                info.message,
                ConvertMessageType(info.type)
            );

            helpBox.name = marker;
            helpBox.style.marginTop = 2;
            helpBox.style.marginBottom = 4;

            int index = searchRoot.IndexOf(insertTarget);
            if (index < 0)
            {
                searchRoot.Add(helpBox);
            }
            else
            {
                searchRoot.Insert(index, helpBox);
            }
        }

        private void ApplyDisplayName(PropertyField propertyField, DisplayNameAttribute displayName)
        {
            propertyField.label = displayName.name;

            Foldout foldout = propertyField.Q<Foldout>();
            if (foldout != null)
            {
                foldout.text = displayName.name;
            }

            Label label = propertyField.Q<Label>();
            if (label == null)
                return;

            label.text = displayName.name;
            label.style.color = displayName.color;
            label.style.fontSize = displayName.fontSize;
            label.style.unityFontStyleAndWeight = displayName.fontStyle;
        }

        private void ApplyEnabledState(
            PropertyField propertyField,
            SerializedProperty property,
            ReadOnlyAttribute readOnly,
            EnableIfAttribute[] enableIfs)
        {
            if (property.propertyPath == "m_Script")
            {
                propertyField.SetEnabled(false);
                return;
            }

            if (readOnly != null)
            {
                propertyField.SetEnabled(false);
                return;
            }

            if (enableIfs != null && enableIfs.Length > 0)
            {
                for (int i = 0; i < enableIfs.Length; i++)
                {
                    EnableIfAttribute enableIf = enableIfs[i];

                    bool pass = EvaluateCondition(
                        property,
                        enableIf.conditionName,
                        enableIf.expectedBool,
                        enableIf.hasCompareValue,
                        enableIf.compareValue
                    );

                    if (!pass)
                    {
                        propertyField.SetEnabled(false);
                        return;
                    }
                }
            }

            propertyField.SetEnabled(true);
        }

        private void ApplyVisibleState(
            VisualElement element,
            SerializedProperty property,
            ShowIfAttribute[] showIfs,
            HideIfAttribute[] hideIfs)
        {
            bool visible = EvaluateVisible(property, showIfs, hideIfs);
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyGeneratedVisibleState(
            PropertyField propertyField,
            SerializedProperty property,
            ShowIfAttribute[] showIfs,
            HideIfAttribute[] hideIfs)
        {
            bool visible = EvaluateVisible(property, showIfs, hideIfs);

            VisualElement targetElement = GetGeneratedVisibleTarget(propertyField);
            targetElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            SetGeneratedInfoVisible(propertyField, visible);
        }

        private VisualElement GetGeneratedVisibleTarget(PropertyField propertyField)
        {
            VisualElement parent = propertyField.parent;

            if (IsPropertyRow(parent))
                return parent;

            return propertyField;
        }

        private void SetGeneratedInfoVisible(PropertyField propertyField, bool visible)
        {
            if (string.IsNullOrEmpty(propertyField.bindingPath))
                return;

            VisualElement searchRoot = propertyField.parent;

            if (IsPropertyRow(searchRoot))
                searchRoot = searchRoot.parent;

            if (searchRoot == null)
                return;

            HelpBox helpBox = searchRoot.Q<HelpBox>(GetInfoMarker(propertyField.bindingPath));
            if (helpBox != null)
            {
                helpBox.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private bool EvaluateVisible(
            SerializedProperty property,
            ShowIfAttribute[] showIfs,
            HideIfAttribute[] hideIfs)
        {
            if (showIfs != null && showIfs.Length > 0)
            {
                for (int i = 0; i < showIfs.Length; i++)
                {
                    ShowIfAttribute showIf = showIfs[i];

                    bool pass = EvaluateCondition(
                        property,
                        showIf.conditionName,
                        showIf.expectedBool,
                        showIf.hasCompareValue,
                        showIf.compareValue
                    );

                    if (!pass)
                        return false;
                }
            }

            if (hideIfs != null && hideIfs.Length > 0)
            {
                for (int i = 0; i < hideIfs.Length; i++)
                {
                    HideIfAttribute hideIf = hideIfs[i];

                    bool shouldHide = EvaluateCondition(
                        property,
                        hideIf.conditionName,
                        hideIf.expectedBool,
                        hideIf.hasCompareValue,
                        hideIf.compareValue
                    );

                    if (shouldHide)
                        return false;
                }
            }

            return true;
        }

        private bool EvaluateCondition(
            SerializedProperty property,
            string conditionName,
            bool expectedBool,
            bool hasCompareValue,
            string compareValue)
        {
            if (string.IsNullOrEmpty(conditionName))
                return true;

            bool invert = false;

            if (conditionName.StartsWith("!"))
            {
                invert = true;
                conditionName = conditionName.Substring(1);
            }

            object owner = GetPropertyOwnerObject(property);

            if (!TryGetConditionValue(owner, conditionName, out object value))
            {
                TryGetConditionValue(target, conditionName, out value);
            }

            bool result = ConvertConditionValue(
                value,
                expectedBool,
                hasCompareValue,
                compareValue
            );

            return invert ? !result : result;
        }

        private bool ConvertConditionValue(
            object value,
            bool expectedBool,
            bool hasCompareValue,
            string compareValue)
        {
            if (hasCompareValue)
            {
                string valueString = value == null ? string.Empty : value.ToString();
                return string.Equals(valueString, compareValue, StringComparison.Ordinal);
            }

            bool result;

            if (value is bool boolValue)
            {
                result = boolValue;
            }
            else if (value is UnityEngine.Object unityObject)
            {
                result = unityObject != null;
            }
            else if (value == null)
            {
                result = false;
            }
            else if (value is int intValue)
            {
                result = intValue != 0;
            }
            else if (value is float floatValue)
            {
                result = Math.Abs(floatValue) > 0.0001f;
            }
            else if (value is double doubleValue)
            {
                result = Math.Abs(doubleValue) > 0.0001d;
            }
            else
            {
                result = true;
            }

            return result == expectedBool;
        }

        private bool TryGetConditionValue(object owner, string conditionName, out object value)
        {
            value = null;

            if (owner == null || string.IsNullOrEmpty(conditionName))
                return false;

            Type type = owner.GetType();

            FieldInfo fieldInfo = GetFieldInfo(type, conditionName);
            if (fieldInfo != null)
            {
                value = fieldInfo.GetValue(owner);
                return true;
            }

            PropertyInfo propertyInfo = type.GetProperty(
                conditionName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (propertyInfo != null && propertyInfo.GetIndexParameters().Length == 0)
            {
                value = propertyInfo.GetValue(owner);
                return true;
            }

            MethodInfo methodInfo = type.GetMethod(
                conditionName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null
            );

            if (methodInfo != null)
            {
                value = methodInfo.Invoke(owner, null);
                return true;
            }

            return false;
        }

        private object GetPropertyOwnerObject(SerializedProperty property)
        {
            if (property == null)
                return null;

            object obj = property.serializedObject.targetObject;

            string path = property.propertyPath.Replace(".Array.data[", "[");
            string[] elements = path.Split('.');

            for (int i = 0; i < elements.Length - 1; i++)
            {
                obj = GetPathValue(obj, elements[i]);

                if (obj == null)
                    return null;
            }

            return obj;
        }

        private object GetPathValue(object source, string path)
        {
            if (source == null)
                return null;

            string fieldName = path;
            int index = -1;

            int bracketIndex = path.IndexOf("[", StringComparison.Ordinal);
            if (bracketIndex >= 0)
            {
                fieldName = path.Substring(0, bracketIndex);

                int endBracketIndex = path.IndexOf("]", StringComparison.Ordinal);
                string indexString = path.Substring(
                    bracketIndex + 1,
                    endBracketIndex - bracketIndex - 1
                );

                int.TryParse(indexString, out index);
            }

            object value = GetMemberValue(source, fieldName);

            if (index >= 0)
                return GetIndexedValue(value, index);

            return value;
        }

        private object GetMemberValue(object source, string memberName)
        {
            if (source == null)
                return null;

            Type type = source.GetType();

            FieldInfo fieldInfo = GetFieldInfo(type, memberName);
            if (fieldInfo != null)
                return fieldInfo.GetValue(source);

            PropertyInfo propertyInfo = type.GetProperty(
                memberName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (propertyInfo != null && propertyInfo.GetIndexParameters().Length == 0)
                return propertyInfo.GetValue(source);

            return null;
        }

        private object GetIndexedValue(object source, int index)
        {
            if (source == null)
                return null;

            if (source is IList list)
            {
                if (index >= 0 && index < list.Count)
                    return list[index];

                return null;
            }

            if (source is IEnumerable enumerable)
            {
                int i = 0;

                foreach (object item in enumerable)
                {
                    if (i == index)
                        return item;

                    i++;
                }
            }

            return null;
        }

        private FieldAttributes GetFieldAttributes(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
                return new FieldAttributes();

            return new FieldAttributes
            {
                Info = fieldInfo.GetCustomAttribute<InfoAttribute>(true),
                DisplayName = fieldInfo.GetCustomAttribute<DisplayNameAttribute>(true),
                ReadOnly = fieldInfo.GetCustomAttribute<ReadOnlyAttribute>(true),
                OpenInspector = fieldInfo.GetCustomAttribute<OpenInspectorAttribute>(true),
                Guid = fieldInfo.GetCustomAttribute<ScriptableObjectGUIDAttribute>(true),

                EnableIfs = fieldInfo.GetCustomAttributes<EnableIfAttribute>(true).ToArray(),
                ShowIfs = fieldInfo.GetCustomAttributes<ShowIfAttribute>(true).ToArray(),
                HideIfs = fieldInfo.GetCustomAttributes<HideIfAttribute>(true).ToArray(),
            };
        }

        private HelpBoxMessageType ConvertMessageType(InfoMessageType type)
        {
            return type switch
            {
                InfoMessageType.None => HelpBoxMessageType.None,
                InfoMessageType.Info => HelpBoxMessageType.Info,
                InfoMessageType.Warning => HelpBoxMessageType.Warning,
                InfoMessageType.Error => HelpBoxMessageType.Error,
                _ => HelpBoxMessageType.Info,
            };
        }

        private FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            while (type != null)
            {
                FieldInfo fieldInfo = type.GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly
                );

                if (fieldInfo != null)
                    return fieldInfo;

                type = type.BaseType;
            }

            return null;
        }

        private FieldInfo GetFieldInfoByPropertyPath(Type rootType, string propertyPath)
        {
            Type currentType = rootType;
            string[] parts = propertyPath.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];

                if (part == "Array")
                {
                    i++;

                    if (i < parts.Length && parts[i].StartsWith("data["))
                    {
                        currentType = GetElementType(currentType);
                    }

                    continue;
                }

                FieldInfo fieldInfo = GetFieldInfo(currentType, part);
                if (fieldInfo == null)
                    return null;

                if (i == parts.Length - 1)
                    return fieldInfo;

                currentType = fieldInfo.FieldType;
            }

            return null;
        }

        private Type GetElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType)
                return type.GetGenericArguments()[0];

            return type;
        }

        private bool IsPropertyRow(VisualElement element)
        {
            return element != null &&
                   !string.IsNullOrEmpty(element.name) &&
                   element.name.StartsWith(RowPrefix, StringComparison.Ordinal);
        }

        private string GetInfoMarker(string bindingPath)
        {
            return InfoPrefix + MakeSafeBindingPath(bindingPath);
        }

        private string MakeSafeBindingPath(string bindingPath)
        {
            if (string.IsNullOrEmpty(bindingPath))
                return string.Empty;

            return bindingPath
                .Replace('.', '_')
                .Replace('[', '_')
                .Replace(']', '_');
        }

        private class FieldAttributes
        {
            public InfoAttribute Info;
            public DisplayNameAttribute DisplayName;
            public ReadOnlyAttribute ReadOnly;
            public OpenInspectorAttribute OpenInspector;
            public ScriptableObjectGUIDAttribute Guid;

            public EnableIfAttribute[] EnableIfs = Array.Empty<EnableIfAttribute>();
            public ShowIfAttribute[] ShowIfs = Array.Empty<ShowIfAttribute>();
            public HideIfAttribute[] HideIfs = Array.Empty<HideIfAttribute>();
        }
    }
}
#endif
