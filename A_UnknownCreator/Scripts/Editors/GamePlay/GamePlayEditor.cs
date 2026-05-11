#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    public enum CfgTypes
    {
        Unit,
        UnitModel,
        Ability,
        Stats,
        StatsGroup,
        Anim,
        Sound,
        TextureTrigger
    }

    public class GamePlayEditor : EditorWindow
    {
        private VisualTreeAsset m_VisualTreeAsset;
        private VisualElement root => rootVisualElement;

        private ListView itemList, groupList;
        private ScrollView content;
        private Label contentName;
        private DropdownField configSelection;
        private TextField jsonPath, filePath;
        private Button openScript;

        private readonly Dictionary<string, Action<bool>> exportActions = new();
        private readonly Dictionary<string, Func<bool>> importActions = new();

        // 当前列表
        private List<CustomScriptableObject> soList = new();

        // 列表组：key = 文件夹路径，value = 该文件夹下的配置
        private readonly Dictionary<string, List<CustomScriptableObject>> groupDict = new();

        // 组名称：key = 文件夹路径，displayName = 显示名
        private List<(string key, string displayName)> groupNames = new();

        private string fileContent;
        private bool isRefreshing;

        private const string nameCfg = "CfgSO"; // 配置尾名（确保资产名称尾部一致）
        private const string SortKeyV2 = nameof(GamePlayEditor) + "_SortKeyV2";
        private const string folderPathKey = nameof(GamePlayEditor) + "_JsonFolderPath";
        private const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const string LastCfgType = nameof(GamePlayEditor) + "_LastCfgType";
        private const string LastGroupKey = nameof(GamePlayEditor) + "_LastGroupKey";
        private const string LastAssetGuid = nameof(GamePlayEditor) + "_LastAssetGuid";

        [MenuItem("UnknownCreator/GamePlayEditor")]
        public static void GamePlay()
        {
            var wnd = GetWindow<GamePlayEditor>();
            wnd.titleContent = new GUIContent("GamePlayEditor");
            wnd.Show();
            wnd.Focus();
        }

        public void CreateGUI()
        {
            InitializeCfgActions();

            root.Clear();

            if (m_VisualTreeAsset == null)
                m_VisualTreeAsset = UnityEditorGlobals.GetAsset<VisualTreeAsset>("GamePlayEditor");

            if (m_VisualTreeAsset == null)
            {
                root.Add(new Label("未找到 GamePlayEditor.uxml"));
                return;
            }

            root.Add(m_VisualTreeAsset.CloneTree());

            itemList = root.Q<ListView>("ItemList");
            groupList = root.Q<ListView>("GroupList");
            content = root.Q<ScrollView>("Content");
            contentName = root.Q<Label>("ContentName");
            configSelection = root.Q<DropdownField>("ConfigSelection");
            filePath = root.Q<TextField>("FilePath");
            jsonPath = root.Q<TextField>("JsonPath");
            openScript = root.Q<Button>("OpenScript");

            if (itemList == null || groupList == null || content == null || contentName == null ||
                configSelection == null || filePath == null || jsonPath == null)
            {
                root.Add(new Label("GamePlayEditor.uxml 缺少必要控件，请检查控件名称。"));
                return;
            }

            if (EditorPrefs.HasKey(folderPathKey))
                jsonPath.value = EditorPrefs.GetString(folderPathKey);

            RegisterButton("OpenScript", OpenScript);
            RegisterButton("FindFile", FindFile);
            RegisterButton("FindJsonPath", FindJsonPath);
            RegisterButton("AddAsset", AddAsset);
            RegisterButton("RemoveAsset", RemoveAsset);
            RegisterButton("Export", () => ExportJson(false));
            RegisterButton("AllExport", () => ExportJson(true));
            RegisterButton("Import", ImportJson);
            RegisterButton("CopyName", () => CopyName(itemList.selectedItem as CustomScriptableObject));
            RegisterButton("Rename", () => Rename(itemList.selectedItem as CustomScriptableObject, itemList.selectedIndex));
            RegisterButton("FocusAsset", () => FocusAsset(itemList.selectedItem as CustomScriptableObject));

            configSelection.choices = new List<string>(Enum.GetNames(typeof(CfgTypes)));
            string lastCfgType = EditorPrefs.GetString(LastCfgType, null);
            configSelection.value = !string.IsNullOrEmpty(lastCfgType) && configSelection.choices.Contains(lastCfgType)
                ? lastCfgType
                : configSelection.choices[0];
            configSelection.RegisterValueChangedCallback(ChangeCfg);

            groupList.selectionType = SelectionType.Single;
            groupList.makeItem = () => new Label();
            groupList.bindItem = (element, index) =>
            {
                if (element is not Label label) return;
                label.text = index >= 0 && index < groupNames.Count ? groupNames[index].displayName : string.Empty;
            };
            groupList.selectionChanged += ChangeGroup;

            itemList.selectionType = SelectionType.Multiple;
            itemList.makeItem = CreateItemElement;
            itemList.bindItem = BindItemElement;
            itemList.selectionChanged += OnSelectionChanged;
            itemList.itemIndexChanged += (_, _) => SaveSort();

            UpdateAbilityOnlyButtons();
            LoadAllAssets(GetCurrentCfgAssetTypeName());
        }

        private void InitializeCfgActions()
        {
            exportActions.Clear();
            importActions.Clear();

            RegisterCfgAction(nameof(CfgTypes.UnitModel), typeof(UnitModelCfgSO), typeof(UnitModelCfg));
            RegisterCfgAction(nameof(CfgTypes.StatsGroup), typeof(StatsGroupCfgSO), typeof(List<OverrideStats>));
            RegisterCfgAction(nameof(CfgTypes.Stats), typeof(StatsCfgSO), typeof(StatsCfg));
            RegisterCfgAction(nameof(CfgTypes.Sound), typeof(SoundCfgSO), typeof(SoundCfg));
            RegisterCfgAction(nameof(CfgTypes.Anim), typeof(AnimCfgSO), typeof(List<AnimCfgInfo>));
            RegisterCfgAction(nameof(CfgTypes.Ability), typeof(AbilityCfgSO), typeof(AbilityCfg));
            RegisterCfgAction(nameof(CfgTypes.Unit), typeof(UnitCfgSO), typeof(UnitCfg));

            // TextureTrigger 如果项目里存在 TextureTriggerCfgSO / TextureTriggerCfg，会自动注册。
            // 如果类型不存在，不会导致编译失败；点击导入/导出时会给出明确提示。
            RegisterCfgActionIfTypesExist(nameof(CfgTypes.TextureTrigger), "TextureTriggerCfgSO", "TextureTriggerCfg");
        }

        private void RegisterCfgAction(string cfgTypeName, Type soType, Type cfgDataType)
        {
            if (string.IsNullOrEmpty(cfgTypeName) || soType == null || cfgDataType == null)
                return;

            if (!typeof(CustomScriptableObject).IsAssignableFrom(soType) || !typeof(ScriptableObject).IsAssignableFrom(soType))
            {
                UCMDebug.LogWarning($"配置类型 {cfgTypeName} 注册失败：{soType.Name} 必须继承 CustomScriptableObject。 ");
                return;
            }

            try
            {
                MethodInfo writeMethod = typeof(GamePlayEditor).GetMethod(nameof(Write), flags)?.MakeGenericMethod(soType, cfgDataType);
                MethodInfo readMethod = typeof(GamePlayEditor).GetMethod(nameof(Read), flags)?.MakeGenericMethod(cfgDataType, soType);

                if (writeMethod == null || readMethod == null)
                {
                    UCMDebug.LogWarning($"配置类型 {cfgTypeName} 注册失败：找不到导入/导出方法。 ");
                    return;
                }

                exportActions[cfgTypeName] = isAll => writeMethod.Invoke(this, new object[] { isAll, "cfg" });
                importActions[cfgTypeName] = () =>
                {
                    object result = readMethod.Invoke(this, null);
                    return result is bool b && b;
                };
            }
            catch (Exception e)
            {
                UCMDebug.LogWarning($"配置类型 {cfgTypeName} 注册失败：{e.Message}");
            }
        }

        private void RegisterCfgActionIfTypesExist(string cfgTypeName, string soTypeName, string cfgDataTypeName)
        {
            Type soType = FindTypeByName(soTypeName);
            Type cfgDataType = FindTypeByName(cfgDataTypeName);

            if (soType == null || cfgDataType == null)
                return;

            RegisterCfgAction(cfgTypeName, soType, cfgDataType);
        }

        private static Type FindTypeByName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type direct = assembly.GetType(typeName);
                if (direct != null) return direct;

                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.Name == typeName)
                            return type;
                    }
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (Type type in e.Types)
                    {
                        if (type != null && type.Name == typeName)
                            return type;
                    }
                }
            }

            return null;
        }

        private void RegisterButton(string buttonName, Action callback)
        {
            Button button = root.Q<Button>(buttonName);
            if (button != null)
                button.clicked += callback;
        }

        private string CurrentCfgType => configSelection?.value;

        private string GetCurrentCfgAssetTypeName()
        {
            return string.IsNullOrEmpty(CurrentCfgType) ? string.Empty : CurrentCfgType + nameCfg;
        }

        private string GetPrefKey(string key)
        {
            return string.IsNullOrEmpty(CurrentCfgType) ? key : $"{key}_{CurrentCfgType}";
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrEmpty(path) ? string.Empty : path.Replace("\\", "/");
        }

        private static string GetAssetGuid(UnityEngine.Object obj)
        {
            if (obj == null) return string.Empty;
            string path = AssetDatabase.GetAssetPath(obj);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        private string GetCurrentGroupKey()
        {
            int index = groupList?.selectedIndex ?? -1;
            return index >= 0 && index < groupNames.Count ? groupNames[index].key : string.Empty;
        }

        private string GetGroupKey(CustomScriptableObject so)
        {
            if (so == null) return string.Empty;
            string path = NormalizeAssetPath(AssetDatabase.GetAssetPath(so));
            return string.IsNullOrEmpty(path) ? string.Empty : NormalizeAssetPath(Path.GetDirectoryName(path));
        }

        private bool IsHiddenItem(CustomScriptableObject so)
        {
            return so != null && CurrentCfgType == nameof(CfgTypes.Ability) && so.name == nameof(AbilityNull);
        }

        private IEnumerable<CustomScriptableObject> GetAllCurrentAssets()
        {
            return groupDict.Values.SelectMany(list => list).Where(so => so != null);
        }

        private int FindFirstSelectableIndex(List<CustomScriptableObject> list)
        {
            if (list == null) return -1;

            for (int i = 0; i < list.Count; i++)
            {
                if (!IsHiddenItem(list[i]))
                    return i;
            }

            return -1;
        }

        private void SaveCurrentSelection(CustomScriptableObject so)
        {
            if (so == null || string.IsNullOrEmpty(CurrentCfgType)) return;

            string groupKey = GetGroupKey(so);
            string guid = GetAssetGuid(so);

            if (!string.IsNullOrEmpty(groupKey))
                EditorPrefs.SetString(GetPrefKey(LastGroupKey), groupKey);

            if (!string.IsNullOrEmpty(guid))
                EditorPrefs.SetString(GetPrefKey(LastAssetGuid), guid);
        }

        private void ClearContentPanel()
        {
            content?.Clear();
            if (contentName != null)
                contentName.text = "配置名称";
        }

        private void CreateAssetsPanel(CustomScriptableObject activeItem)
        {
            if (activeItem == null || IsHiddenItem(activeItem)) return;

            content.Clear();
            contentName.text = activeItem.name;

            var inspector = new InspectorElement();
            content.Add(inspector);
            inspector.Bind(new SerializedObject(activeItem));

            SaveCurrentSelection(activeItem);
        }

        private void LoadAllAssets(string assetTypeName, string preferredGroupKey = null, string preferredAssetGuid = null)
        {
            isRefreshing = true;

            try
            {
                soList = new List<CustomScriptableObject>();
                groupDict.Clear();
                groupNames.Clear();

                if (string.IsNullOrEmpty(assetTypeName))
                {
                    RefreshListViews();
                    ClearContentPanel();
                    return;
                }

                string[] guids = AssetDatabase.FindAssets("t:" + assetTypeName, null);
                foreach (string guid in guids)
                {
                    string path = NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(guid));
                    var so = AssetDatabase.LoadAssetAtPath<CustomScriptableObject>(path);
                    if (so == null) continue;

                    string folder = NormalizeAssetPath(Path.GetDirectoryName(path));
                    string groupDisplayName = string.IsNullOrEmpty(folder) ? "未分组" : Path.GetFileName(folder);

                    if (!groupDict.TryGetValue(folder, out var list))
                    {
                        list = new List<CustomScriptableObject>();
                        groupDict[folder] = list;
                        groupNames.Add((folder, groupDisplayName));
                    }

                    list.Add(so);
                }

                groupNames = groupNames
                    .OrderBy(g => g.displayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(g => g.key, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                ApplySavedSort();

                groupList.itemsSource = groupNames;
                groupList.RefreshItems();

                if (groupNames.Count == 0)
                {
                    itemList.itemsSource = soList;
                    itemList.ClearSelection();
                    itemList.RefreshItems();
                    ClearContentPanel();
                    return;
                }

                string savedGroupKey = !string.IsNullOrEmpty(preferredGroupKey)
                    ? preferredGroupKey
                    : EditorPrefs.GetString(GetPrefKey(LastGroupKey), string.Empty);

                int groupIndex = groupNames.FindIndex(g => g.key == savedGroupKey);
                if (groupIndex < 0) groupIndex = 0;

                SetGroupAndItem(groupIndex, preferredAssetGuid);
            }
            finally
            {
                isRefreshing = false;
            }
        }

        private void RefreshListViews()
        {
            if (groupList != null)
            {
                groupList.itemsSource = groupNames;
                groupList.RefreshItems();
            }

            if (itemList != null)
            {
                itemList.itemsSource = soList;
                itemList.RefreshItems();
            }
        }

        private void SetGroupAndItem(int groupIndex, string preferredAssetGuid = null)
        {
            if (groupIndex < 0 || groupIndex >= groupNames.Count)
            {
                soList = new List<CustomScriptableObject>();
                itemList.itemsSource = soList;
                itemList.ClearSelection();
                itemList.RefreshItems();
                ClearContentPanel();
                return;
            }

            string groupKey = groupNames[groupIndex].key;
            if (!groupDict.TryGetValue(groupKey, out var list))
                list = new List<CustomScriptableObject>();

            soList = list;

            groupList.SetSelection(groupIndex);
            itemList.itemsSource = soList;
            itemList.ClearSelection();
            itemList.RefreshItems();

            EditorPrefs.SetString(GetPrefKey(LastGroupKey), groupKey);

            string assetGuid = !string.IsNullOrEmpty(preferredAssetGuid)
                ? preferredAssetGuid
                : EditorPrefs.GetString(GetPrefKey(LastAssetGuid), string.Empty);

            int itemIndex = -1;
            if (!string.IsNullOrEmpty(assetGuid))
                itemIndex = soList.FindIndex(so => GetAssetGuid(so) == assetGuid && !IsHiddenItem(so));

            if (itemIndex < 0)
                itemIndex = FindFirstSelectableIndex(soList);

            if (itemIndex >= 0)
            {
                itemList.AddToSelection(itemIndex);
                itemList.ScrollToItem(itemIndex);
                CreateAssetsPanel(soList[itemIndex]);
            }
            else
            {
                ClearContentPanel();
            }
        }

        private void ChangeGroup(IEnumerable<object> selectedItems)
        {
            if (isRefreshing || selectedItems == null || !selectedItems.Any()) return;

            var selectedTuple = (ValueTuple<string, string>)selectedItems.First();
            string groupKey = selectedTuple.Item1;

            int groupIndex = groupNames.FindIndex(g => g.key == groupKey);
            if (groupIndex < 0) return;

            isRefreshing = true;
            try
            {
                SetGroupAndItem(groupIndex);
            }
            finally
            {
                isRefreshing = false;
            }
        }

        private void ChangeCfg(ChangeEvent<string> value)
        {
            if (value == null || string.IsNullOrEmpty(value.newValue)) return;

            EditorPrefs.SetString(LastCfgType, value.newValue);
            ClearContentPanel();
            itemList.ClearSelection();
            groupList.ClearSelection();
            UpdateAbilityOnlyButtons();
            LoadAllAssets(value.newValue + nameCfg);
        }

        private void UpdateAbilityOnlyButtons()
        {
            if (openScript == null) return;
            openScript.style.display = CurrentCfgType == nameof(CfgTypes.Ability) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement CreateItemElement()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;

            var icon = new VisualElement { name = "Icon", style = { width = 24, height = 24, marginTop = 7 } };
            container.Add(icon);

            var nameLabel = new Label { name = "Name", style = { marginTop = 7 } };
            container.Add(nameLabel);

            var hideMark = new VisualElement { name = "Hide" };
            container.Add(hideMark);

            return container;
        }

        private void BindItemElement(VisualElement e, int i)
        {
            var icon = e.Q<VisualElement>("Icon");
            var nameLabel = e.Q<Label>("Name");
            var hideMark = e.Q<VisualElement>("Hide");

            if (i < 0 || i >= soList.Count || soList[i] == null)
            {
                if (icon != null) icon.style.backgroundImage = null;
                if (nameLabel != null) nameLabel.text = string.Empty;
                if (hideMark != null) hideMark.style.display = DisplayStyle.None;
                e.SetEnabled(false);
                return;
            }

            var currentSO = soList[i];
            bool isHidden = IsHiddenItem(currentSO);

            if (icon != null)
                icon.style.backgroundImage = currentSO is AbilityCfgSO abilitySO ? abilitySO.icon?.editorAsset : null;

            if (nameLabel != null)
                nameLabel.text = currentSO.name;

            if (hideMark != null)
                hideMark.style.display = isHidden ? DisplayStyle.Flex : DisplayStyle.None;

            e.SetEnabled(!isHidden);
        }

        private void OnSelectionChanged(IEnumerable<object> items)
        {
            if (isRefreshing) return;

            var first = items?.FirstOrDefault() as CustomScriptableObject;
            if (first == null || IsHiddenItem(first))
            {
                itemList.ClearSelection();
                ClearContentPanel();
                return;
            }

            CreateAssetsPanel(first);
        }

        private void SaveSort()
        {
            if (string.IsNullOrEmpty(CurrentCfgType) || soList == null || soList.Count == 0) return;

            string groupKey = GetCurrentGroupKey();
            if (string.IsNullOrEmpty(groupKey)) return;

            Dictionary<string, List<string>> sortDict = LoadSortDict();
            string sortKey = GetSortKey(CurrentCfgType, groupKey);

            sortDict[sortKey] = soList
                .Where(so => so != null && !IsHiddenItem(so))
                .Select(GetAssetGuid)
                .Where(guid => !string.IsNullOrEmpty(guid))
                .ToList();

            EditorPrefs.SetString(SortKeyV2, JsonMapper.ToJson(sortDict));
        }

        private void ApplySavedSort()
        {
            Dictionary<string, List<string>> sortDict = LoadSortDict();

            foreach (var kv in groupDict)
            {
                string sortKey = GetSortKey(CurrentCfgType, kv.Key);
                List<string> sortedGuids = sortDict.TryGetValue(sortKey, out var saved) ? saved : null;
                Dictionary<string, int> orderMap = sortedGuids == null
                    ? new Dictionary<string, int>()
                    : sortedGuids.Select((guid, index) => new { guid, index })
                        .GroupBy(x => x.guid)
                        .ToDictionary(g => g.Key, g => g.First().index);

                kv.Value.Sort((a, b) =>
                {
                    int aOrder = orderMap.TryGetValue(GetAssetGuid(a), out int ai) ? ai : int.MaxValue;
                    int bOrder = orderMap.TryGetValue(GetAssetGuid(b), out int bi) ? bi : int.MaxValue;

                    if (aOrder != bOrder)
                        return aOrder.CompareTo(bOrder);

                    return string.Compare(a?.name, b?.name, StringComparison.OrdinalIgnoreCase);
                });
            }
        }

        private static string GetSortKey(string cfgType, string groupKey)
        {
            return $"{cfgType}|{groupKey}";
        }

        private Dictionary<string, List<string>> LoadSortDict()
        {
            if (!EditorPrefs.HasKey(SortKeyV2))
                return new Dictionary<string, List<string>>();

            try
            {
                return JsonMapper.ToObject<Dictionary<string, List<string>>>(EditorPrefs.GetString(SortKeyV2))
                       ?? new Dictionary<string, List<string>>();
            }
            catch
            {
                return new Dictionary<string, List<string>>();
            }
        }

        private void SelectSO(CustomScriptableObject so, bool focusAsset = true)
        {
            if (so == null || IsHiddenItem(so)) return;

            string groupKey = GetGroupKey(so);
            int groupIndex = groupNames.FindIndex(g => g.key == groupKey);
            if (groupIndex < 0) return;

            string assetGuid = GetAssetGuid(so);

            isRefreshing = true;
            try
            {
                SetGroupAndItem(groupIndex, assetGuid);
            }
            finally
            {
                isRefreshing = false;
            }

            if (focusAsset)
                FocusAsset(so);
        }

        #region 功能栏

        private void FindFile()
        {
            string searchText = filePath.value;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                EditorUtility.DisplayDialog("错误", "请输入搜索内容！", "确定");
                return;
            }

            var matches = GetAllCurrentAssets()
                .Where(so => !IsHiddenItem(so) && so.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(so => so.name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (matches.Count == 0)
            {
                EditorUtility.DisplayDialog("未找到配置", $"未找到匹配“{searchText}”的配置", "确定");
                return;
            }

            if (matches.Count == 1)
                SelectSO(matches[0]);
            else
                SearchResultWindow.ShowWindow(matches, so => SelectSO(so));
        }

        private void FindJsonPath()
        {
            string folderPath = EditorUtility.OpenFolderPanel("选择一个文件夹", "", "");
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                jsonPath.value = folderPath;
                EditorPrefs.SetString(folderPathKey, folderPath);
            }
        }

        private void AddAsset()
        {
            string className = GetCurrentCfgAssetTypeName();
            if (string.IsNullOrEmpty(className))
            {
                EditorUtility.DisplayDialog("错误", "没有选择配置类型！", "确定");
                return;
            }

            var cfg = UnityEditorGlobals.Create(className, "New" + className) as CustomScriptableObject;
            if (cfg == null)
            {
                EditorUtility.DisplayDialog("错误", $"创建 {className} 失败，请确认类型存在。", "确定");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string cfgGuid = GetAssetGuid(cfg);
            string groupKey = GetGroupKey(cfg);
            LoadAllAssets(className, groupKey, cfgGuid);
            SelectSO(cfg);

            if (CurrentCfgType == nameof(CfgTypes.Ability))
                TryCreateAbilityScript(cfg);
        }

        private void TryCreateAbilityScript(CustomScriptableObject cfg)
        {
            if (cfg == null) return;

            string abilityName = cfg.name;
            if (!IsValidCSharpIdentifier(abilityName))
            {
                EditorUtility.DisplayDialog("提示", $"已创建配置【{abilityName}】，但名称不是合法 C# 类名，未自动生成脚本。", "确定");
                return;
            }

            string absolutePath = EditorUtility.OpenFolderPanel("选择脚本存放目录", Application.dataPath, "");
            if (string.IsNullOrEmpty(absolutePath))
                return;

            absolutePath = NormalizeAssetPath(absolutePath);
            string dataPath = NormalizeAssetPath(Application.dataPath);

            if (!absolutePath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("错误", "必须选择在 Assets 内的文件夹！", "确定");
                return;
            }

            string relativePath = "Assets" + absolutePath.Substring(dataPath.Length);
            string scriptPath = NormalizeAssetPath(Path.Combine(relativePath, $"{abilityName}.cs"));

            if (File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("提示", $"脚本已存在：{scriptPath}", "确定");
                return;
            }

            string scriptContent = $@"using UnityEngine;
using UnknownCreator.Modules;

public class {abilityName} : AbilityBase
{{
    public override void OnCreated()
    {{

    }}

    protected override void OnRelease()
    {{

    }}
}}
";

            try
            {
                File.WriteAllText(scriptPath, scriptContent);
                AssetDatabase.Refresh();
                ShowNotification(new GUIContent("✓ 已生成 Ability 脚本"),1);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"生成脚本失败：{e.Message}", "确定");
            }
        }

        private static bool IsValidCSharpIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (!char.IsLetter(name[0]) && name[0] != '_') return false;

            for (int i = 1; i < name.Length; i++)
            {
                if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                    return false;
            }

            return true;
        }

        private void RemoveAsset()
        {
            var selectedItems = itemList.selectedItems?
                .OfType<CustomScriptableObject>()
                .Where(so => so != null && !IsHiddenItem(so))
                .Distinct()
                .ToList();

            if (selectedItems == null || selectedItems.Count == 0)
            {
                EditorUtility.DisplayDialog("删除提示", "没有选择任何可删除配置！", "确定");
                return;
            }

            string message = selectedItems.Count == 1
                ? $"确定要删除配置【{selectedItems[0].name}】吗？"
                : $"确定要删除选中的 {selectedItems.Count} 个配置吗？";

            if (!EditorUtility.DisplayDialog("删除确认", message, "确定", "取消"))
                return;

            string currentGroupKey = GetCurrentGroupKey();
            int firstSelectedIndex = selectedItems
                .Select(so => soList.IndexOf(so))
                .Where(index => index >= 0)
                .DefaultIfEmpty(0)
                .Min();

            int failedCount = 0;
            foreach (var obj in selectedItems)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                {
                    failedCount++;
                    continue;
                }

                bool success = AssetDatabase.DeleteAsset(path);
                if (!success)
                {
                    failedCount++;
                    UCMDebug.LogWarning($"删除失败：{path}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ClearContentPanel();
            EditorPrefs.DeleteKey(GetPrefKey(LastAssetGuid));

            LoadAllAssets(GetCurrentCfgAssetTypeName(), currentGroupKey);

            if (soList.Count > 0)
            {
                int nextIndex = Mathf.Clamp(firstSelectedIndex, 0, soList.Count - 1);
                if (IsHiddenItem(soList[nextIndex]))
                    nextIndex = FindFirstSelectableIndex(soList);

                if (nextIndex >= 0)
                    SelectSO(soList[nextIndex], false);
            }

            if (failedCount > 0)
                EditorUtility.DisplayDialog("删除完成", $"部分配置删除失败：{failedCount} 个，请查看 Console。", "确定");
        }

        private void CopyName(CustomScriptableObject so)
        {
            if (so == null || IsHiddenItem(so)) return;
            GUIUtility.systemCopyBuffer = so.name;
            UCMDebug.Log($"已复制名称: {so.name}");
        }

        private void Rename(CustomScriptableObject so, int index)
        {
            if (so == null || IsHiddenItem(so)) return;

            RenameWindow.ShowPanel(so.name, newName =>
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    EditorUtility.DisplayDialog("错误", "名称不能为空！", "确定");
                    return;
                }

                if (newName == so.name)
                    return;

                bool duplicated = GetAllCurrentAssets()
                    .Any(x => x != so && string.Equals(x.name, newName, StringComparison.OrdinalIgnoreCase));

                if (duplicated)
                {
                    EditorUtility.DisplayDialog("错误", "同类型配置中已经存在同名资源。JSON 使用名称作为 Key，不建议重名。", "确定");
                    return;
                }

                string path = AssetDatabase.GetAssetPath(so);
                string error = AssetDatabase.RenameAsset(path, newName);
                if (!string.IsNullOrEmpty(error))
                {
                    EditorUtility.DisplayDialog("错误", $"重命名失败：{error}", "确定");
                    return;
                }

                so.name = newName;
                EditorUtility.SetDirty(so);
                contentName.text = newName;

                if (index >= 0 && index < soList.Count)
                    itemList.RefreshItem(index);
                else
                    itemList.RefreshItems();

                SaveSort();
                SaveCurrentSelection(so);
                AssetDatabase.SaveAssets();
            });
        }

        private void FocusAsset(CustomScriptableObject so)
        {
            if (so == null) return;
            string path = AssetDatabase.GetAssetPath(so);
            if (string.IsNullOrEmpty(path)) return;

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<CustomScriptableObject>(path);
            EditorUtility.FocusProjectWindow();
        }

        private void OpenScript()
        {
            var so = itemList.selectedItem as AbilityCfgSO;
            if (so == null) return;

            if (so.cfgScript != null)
            {
                AssetDatabase.OpenAsset(so.cfgScript);
                return;
            }

            string soName = so.name;
            string[] guids = AssetDatabase.FindAssets($"{soName} t:MonoScript");

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                if (fileName != soName) continue;

                var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (scriptAsset != null)
                {
                    AssetDatabase.OpenAsset(scriptAsset);
                    return;
                }
            }

            EditorUtility.DisplayDialog("错误", $"无法找到脚本 {soName}！", "确定");
        }

        #endregion

        #region JSON

        private void ExportJson(bool isAll = false)
        {
            if (isAll)
            {
                if (GetAllCurrentAssets().All(IsHiddenItem))
                {
                    EditorUtility.DisplayDialog("错误", "没有可导出的配置！", "确定");
                    return;
                }
            }
            else
            {
                var items = itemList.selectedItems;
                if (items == null || !items.OfType<CustomScriptableObject>().Any(so => so != null && !IsHiddenItem(so)))
                {
                    EditorUtility.DisplayDialog("错误", "没有选择配置！", "确定");
                    return;
                }
            }

            if (!exportActions.TryGetValue(CurrentCfgType, out var action))
            {
                EditorUtility.DisplayDialog("错误", $"未注册 {CurrentCfgType} 的导出逻辑！", "确定");
                return;
            }

            try
            {
                action(isAll);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (TargetInvocationException e)
            {
                EditorUtility.DisplayDialog("错误", $"导出失败：{e.InnerException?.Message ?? e.Message}", "确定");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"导出失败：{e.Message}", "确定");
            }
        }

        private void ImportJson()
        {
            if (!importActions.TryGetValue(CurrentCfgType, out var action))
            {
                EditorUtility.DisplayDialog("错误", $"未注册 {CurrentCfgType} 的导入逻辑！", "确定");
                return;
            }

            string path = EditorUtility.OpenFilePanel("选择文件", "", "json");
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("错误", "导入取消或内容无效！", "确定");
                return;
            }

            try
            {
                fileContent = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"读取 JSON 失败：{e.Message}", "确定");
                return;
            }

            bool success;
            try
            {
                success = action();
            }
            catch (TargetInvocationException e)
            {
                EditorUtility.DisplayDialog("错误", $"导入失败：{e.InnerException?.Message ?? e.Message}", "确定");
                return;
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"导入失败：{e.Message}", "确定");
                return;
            }

            if (!success)
                return;

            LoadAllAssets(GetCurrentCfgAssetTypeName());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("✓ 导入完成"),1);
        }

        private void Write<T, T2>(bool isAll = false, string name = "cfg")
            where T : CustomScriptableObject
            where T2 : class
        {
            var items = isAll
                ? GetAllCurrentAssets().OfType<T>().Where(so => !IsHiddenItem(so)).ToList()
                : itemList.selectedItems.OfType<T>().Where(so => !IsHiddenItem(so)).ToList();

            if (items.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "没有可导出的配置！", "确定");
                return;
            }

            var duplicatedNames = items
                .GroupBy(item => item.name)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicatedNames.Count > 0)
            {
                EditorUtility.DisplayDialog("错误", "存在重名配置，无法安全导出：" + string.Join(", ", duplicatedNames), "确定");
                return;
            }

            var dict = new Dictionary<string, T2>();

            foreach (var item in items)
            {
                if (item == null) continue;

                object value = GetCfgObjectValue(item, name);
                if (value is T2 typedValue)
                {
                    dict[item.name] = typedValue;
                }
                else
                {
                    UCMDebug.LogWarning($"跳过导出【{item.name}】：找不到 {name} 字段/属性，或类型不是 {typeof(T2).Name}。 ");
                }
            }

            if (dict.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "没有成功读取到任何 cfg 数据，导出取消！", "确定");
                return;
            }

            string savePath = Directory.Exists(jsonPath.value)
                ? EditorUtility.SaveFilePanel("保存Json文件", jsonPath.value, CurrentCfgType, "json")
                : EditorUtility.SaveFilePanelInProject("保存Json文件", CurrentCfgType, "json", "请输入文件名以保存JSON数据");

            if (string.IsNullOrEmpty(savePath))
            {
                EditorUtility.DisplayDialog("警告", "保存取消或内容无效！", "确定");
                return;
            }

            try
            {
                File.WriteAllText(savePath, JsonMapper.ToJson(dict));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                ShowNotification(new GUIContent("✓ JSON 文件已保存"),1);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"保存 JSON 失败：{e.Message}", "确定");
            }
        }

        private static object GetCfgObjectValue(UnityEngine.Object item, string name)
        {
            if (item == null) return null;

            Type type = item.GetType();
            FieldInfo field = type.GetField(name, flags);
            if (field != null)
                return field.GetValue(item);

            PropertyInfo property = type.GetProperty(name, flags);
            return property?.GetValue(item);
        }

        private bool Read<T, Y>() where Y : ScriptableObject
        {
            Dictionary<string, T> cfg;
            try
            {
                cfg = JsonMapper.ToObject<Dictionary<string, T>>(fileContent);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"JSON解析失败：{e.Message}", "确定");
                return false;
            }

            if (cfg == null || cfg.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "JSON内容为空或格式不正确！", "确定");
                return false;
            }

            foreach (var item in cfg)
            {
                if (string.IsNullOrWhiteSpace(item.Key)) continue;

                Y data = FindAssetByExactName<Y>(item.Key);
                bool createdNew = false;

                if (data == null)
                {
                    data = UnityEditorGlobals.Create<Y>(item.Key);
                    createdNew = true;
                }

                if (data == null)
                {
                    UCMDebug.LogWarning($"创建 {typeof(Y).Name} 配置【{item.Key}】失败。 ");
                    continue;
                }

                SetCfgValue(data, item.Value);
                EditorUtility.SetDirty(data);

                if (createdNew)
                    UCMDebug.Log($"创建新{typeof(Y).Name}配置【{item.Key}】数据");
                else
                    UCMDebug.Log($"已更新{typeof(Y).Name}配置【{item.Key}】数据");
            }

            return true;
        }

        private static Y FindAssetByExactName<Y>(string assetName) where Y : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets(assetName + " t:" + typeof(Y).Name, null);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Y asset = AssetDatabase.LoadAssetAtPath<Y>(path);
                if (asset != null && asset.name == assetName)
                    return asset;
            }

            return null;
        }

        private void SetCfgValue<Y, T>(Y obj, T value) where Y : ScriptableObject
        {
            if (obj == null) return;

            Type type = typeof(Y);
            FieldInfo fieldInfo = type.GetField("cfg", flags);

            if (fieldInfo != null)
            {
                if (value == null || fieldInfo.FieldType.IsAssignableFrom(typeof(T)))
                {
                    fieldInfo.SetValue(obj, value);
                    return;
                }

                UCMDebug.LogWarning($"配置【{obj.name}】cfg 字段类型不匹配：字段 {fieldInfo.FieldType.Name}，导入 {typeof(T).Name}");
                return;
            }

            PropertyInfo propertyInfo = type.GetProperty("cfg", flags);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                if (value == null || propertyInfo.PropertyType.IsAssignableFrom(typeof(T)))
                {
                    propertyInfo.SetValue(obj, value);
                    return;
                }

                UCMDebug.LogWarning($"配置【{obj.name}】cfg 属性类型不匹配：属性 {propertyInfo.PropertyType.Name}，导入 {typeof(T).Name}");
                return;
            }

            string message = $"配置【{obj.name}】类型 {type.FullName} 中不存在可写入的 'cfg' 字段或属性！";
            UCMDebug.LogWarning(message);
        }

        #endregion
    }

    public class RenameWindow : EditorWindow
    {
        private TextField renameField;

        public static void ShowPanel(string currentName, Action<string> renameCallback)
        {
            var window = GetWindow<RenameWindow>("修改名称");
            window.minSize = new Vector2(300, 100);
            window.CreateUI(currentName, renameCallback);
        }

        private void CreateUI(string currentName, Action<string> renameCallback)
        {
            rootVisualElement.Clear();

            renameField = new TextField("输入新的名称:") { value = currentName };
            rootVisualElement.Add(renameField);

            rootVisualElement.Add(new Button(() =>
            {
                string newName = renameField.value?.Trim();
                if (!string.IsNullOrEmpty(newName))
                    renameCallback?.Invoke(newName);
                Close();
            })
            { text = "确认" });

            rootVisualElement.Add(new Button(() => Close()) { text = "取消" });
        }
    }

    public class SearchResultWindow : EditorWindow
    {
        private Action<CustomScriptableObject> onSelect;
        private List<CustomScriptableObject> results;
        private ScrollView scrollView;

        public static void ShowWindow(List<CustomScriptableObject> results, Action<CustomScriptableObject> onSelect)
        {
            var window = CreateInstance<SearchResultWindow>();
            window.results = results;
            window.onSelect = onSelect;
            window.titleContent = new GUIContent("搜索结果");
            window.minSize = new Vector2(300, 300);
            window.ShowUtility();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            scrollView = new ScrollView();
            rootVisualElement.Add(scrollView);
            RefreshResults();
        }

        private void RefreshResults()
        {
            if (scrollView == null) return;

            scrollView.Clear();

            if (results == null || results.Count == 0)
            {
                scrollView.Add(new Label("无匹配项"));
                return;
            }

            foreach (var so in results.Where(so => so != null))
            {
                Button btn = new Button(() =>
                {
                    onSelect?.Invoke(so);
                    Close();
                })
                {
                    text = so.name
                };
                scrollView.Add(btn);
            }
        }

        public void UpdateResults(List<CustomScriptableObject> newResults)
        {
            results = newResults;
            RefreshResults();
        }

    }


}
#endif
