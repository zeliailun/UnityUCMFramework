#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
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


        //当前列表
        private static List<CustomScriptableObject> soList = new();

        //列表组
        private Dictionary<string, List<CustomScriptableObject>> groupDict = new();

        //组名称
        private List<(string key, string displayName)> groupNames = new();

        // 保存每个配置类型的选中索引
        private Dictionary<string, int> selectedIndexDict = new();

        private Dictionary<string, Action<bool>> exportActions;
        private Dictionary<string, Action> importActions;

        private const string nameCfg = "CfgSO"; //配置尾名（确保资产名称尾部一致）
        private const string SortKey = nameof(SortKey);
        private const string folderPathKey = nameof(folderPathKey);
        private const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private string fileContent;

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
            exportActions = new()
            {
                { nameof(CfgTypes.UnitModel), b => Write<UnitModelCfgSO, UnitModelCfg>(b) },
                { nameof(CfgTypes.StatsGroup), b => Write<StatsGroupCfgSO, List<OverrideStats>>(b) },
                { nameof(CfgTypes.Stats), b => Write<StatsCfgSO, StatsCfg>(b) },
                { nameof(CfgTypes.Sound), b => Write<SoundCfgSO, SoundCfg>(b) },
                { nameof(CfgTypes.Anim), b => Write<AnimCfgSO, List<AnimCfgInfo>>(b) },
                { nameof(CfgTypes.Ability), b => Write<AbilityCfgSO, AbilityCfg>(b) },
                { nameof(CfgTypes.Unit), b => Write<UnitCfgSO, UnitCfg>(b) }
            };

            importActions = new Dictionary<string, Action>
            {
                { nameof(CfgTypes.UnitModel), () => Read<UnitModelCfg, UnitModelCfgSO>() },
                { nameof(CfgTypes.Unit), () => Read<UnitCfg, UnitCfgSO>() },
                { nameof(CfgTypes.Sound), () => Read<SoundCfg, SoundCfgSO>() },
                { nameof(CfgTypes.Anim), () => Read<List<AnimCfgInfo>, AnimCfgSO>() },
                { nameof(CfgTypes.Ability), () => Read<AbilityCfg, AbilityCfgSO>() },
                { nameof(CfgTypes.Stats), () => Read<StatsCfg, StatsCfgSO>() },
                { nameof(CfgTypes.StatsGroup), () => Read<List<OverrideStats>, StatsGroupCfgSO>() }
            };

            if (m_VisualTreeAsset == null)
                m_VisualTreeAsset = UnityEditorGlobals.GetAsset<VisualTreeAsset>("GamePlayEditor");
            root.Add(m_VisualTreeAsset.CloneTree());

            itemList = root.Q<ListView>("ItemList");
            groupList = root.Q<ListView>("GroupList");
            content = root.Q<ScrollView>("Content");
            contentName = root.Q<Label>("ContentName");
            configSelection = root.Q<DropdownField>("ConfigSelection");
            filePath = root.Q<TextField>("FilePath");
            jsonPath = root.Q<TextField>("JsonPath");
            if (EditorPrefs.HasKey(folderPathKey))
                jsonPath.value = EditorPrefs.GetString(folderPathKey);

            root.Q<Button>("FindFile").clicked += FindFile;
            root.Q<Button>("FindJsonPath").clicked += FindJsonPath;
            root.Q<Button>("AddAsset").clicked += AddAsset;
            root.Q<Button>("RemoveAsset").clicked += RemoveAsset;
            root.Q<Button>("Export").clicked += ExportJson;
            root.Q<Button>("Import").clicked += ImportJson;
            root.Q<Button>("AllExport").clicked += ExportCurrentCfgAllJson;
            root.Q<Button>("CopyName").clicked += () => CopyName(itemList.selectedItem as CustomScriptableObject);
            root.Q<Button>("Rename").clicked += () => Rename(itemList.selectedItem as CustomScriptableObject, itemList.selectedIndex);
            root.Q<Button>("FocusAsset").clicked += () => FocusAsset(itemList.selectedItem as CustomScriptableObject);


            groupList.selectionType = SelectionType.Single;
            itemList.selectionType = SelectionType.Multiple;

            // 配置类型下拉
            configSelection.choices = Enum.GetValues(typeof(CfgTypes)).Cast<CfgTypes>().Select(e => e.ToString()).ToList();
            configSelection.value = configSelection.choices[0];
            configSelection.RegisterValueChangedCallback(ChangeCfg);

            // 绑定 groupList
            groupList.makeItem = () => new Label();
            groupList.bindItem = (element, index) =>
            {
                (element as Label).text = groupNames[index].displayName;
            };
            groupList.selectionChanged += ChangeGroup;

            // 绑定 itemList
            itemList.makeItem = () =>
            {
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;

                // 图标
                var icon = new VisualElement { name = "Icon", style = { width = 24, height = 24 } };
                icon.style.marginTop = 7;
                container.Add(icon);

                // 名称
                var nameLabel = new Label { name = "Name" };
                nameLabel.style.marginTop = 7;
                container.Add(nameLabel);

                // 隐藏标记
                var hideMark = new VisualElement { name = "Hide" };
                container.Add(hideMark);

                return container;
            };

            itemList.bindItem = (e, i) =>
            {
                if (soList.IsValid() && i < soList.Count)
                {
                    var currentSO = soList[i];

                    var icon = e.Q<VisualElement>("Icon");
                    var nameLabel = e.Q<Label>("Name");
                    var hideMark = e.Q<VisualElement>("Hide");

                    if (currentSO is AbilityCfgSO abilitySO)
                        icon.style.backgroundImage = abilitySO.icon;
                    else
                        icon.style.backgroundImage = null;

                    nameLabel.text = currentSO.name;

                    // AbilityNull 特殊处理
                    if (currentSO.name == nameof(AbilityNull))
                    {
                        hideMark.style.display = DisplayStyle.Flex;
                        e.SetEnabled(false); // 禁用点击和拖动
                    }
                    else
                    {
                        hideMark.style.display = DisplayStyle.None;
                        e.SetEnabled(true);
                    }
                }
            };

            itemList.selectionChanged += items =>
            {
                var first = items.FirstOrDefault() as CustomScriptableObject;
                if (first == null || first.name == nameof(AbilityNull))
                {
                    itemList.ClearSelection();
                    return;
                }
                CreateAssetsPanel(first);
            };
            itemList.itemIndexChanged += (oldIndex, newIndex) => SaveSort();

            LoadAllAssets(configSelection.value + "CfgSO");
        }

        private void CreateAssetsPanel(CustomScriptableObject activeItem)
        {
            if (activeItem == null) return;
            content.Clear();
            contentName.text = activeItem.name;
            content.Add(new InspectorElement(activeItem));

            int index = soList.IndexOf(activeItem);
            if (index >= 0)
                selectedIndexDict[configSelection.value] = index;

            itemList.RefreshItems();
        }

        private void LoadAllAssets(string name)
        {
            soList.Clear();
            groupDict.Clear();
            groupNames.Clear();

            string[] guids = AssetDatabase.FindAssets("t:" + name, null);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<CustomScriptableObject>(path);
                string folder = Path.GetDirectoryName(path).Replace("\\", "/");
                string groupKey = folder;
                string groupDisplayName = Path.GetFileName(folder);

                if (!groupDict.TryGetValue(groupKey, out var list))
                {
                    list = new List<CustomScriptableObject>();
                    groupDict[groupKey] = list;
                    groupNames.Add((groupKey, groupDisplayName));
                }
                list.Add(so);
            }

            // 恢复排序
            if (EditorPrefs.HasKey(SortKey))
            {
                var sortJson = EditorPrefs.GetString(SortKey);
                var sortDict = JsonMapper.ToObject<Dictionary<string, List<string>>>(sortJson);
                if (sortDict != null && sortDict.TryGetValue(configSelection.value, out var sortedNames))
                {
                    foreach (var group in groupDict.Values)
                    {
                        group.Sort((a, b) =>
                        {
                            int aIndex = sortedNames.IndexOf(a.name);
                            int bIndex = sortedNames.IndexOf(b.name);
                            if (aIndex == -1 && bIndex == -1) return 0;
                            if (aIndex == -1) return 1;
                            if (bIndex == -1) return -1;
                            return aIndex.CompareTo(bIndex);
                        });
                    }
                }
            }

            // 默认选中第一个分组
            if (groupNames.Count > 0)
            {
                var firstKey = groupNames[0].key;
                soList = groupDict[firstKey];
                itemList.itemsSource = soList;
                itemList.RefreshItems();
                groupList.itemsSource = groupNames;
                groupList.RefreshItems();
                groupList.selectedIndex = 0;

                // 恢复上次选中
                if (selectedIndexDict.TryGetValue(configSelection.value, out int savedIndex))
                {
                    if (savedIndex >= 0 && savedIndex < soList.Count)
                    {
                        itemList.ClearSelection();
                        itemList.AddToSelection(savedIndex);
                        itemList.ScrollToItem(savedIndex);
                        CreateAssetsPanel(soList[savedIndex]);
                    }
                }
            }
        }

        private void ChangeGroup(IEnumerable<object> selectedItems)
        {
            if (!selectedItems.Any()) return;
            var selectedTuple = (ValueTuple<string, string>)selectedItems.First();
            string groupKey = selectedTuple.Item1;

            if (groupDict.TryGetValue(groupKey, out var list))
            {
                itemList.ClearSelection();
                soList = list;
                itemList.itemsSource = soList;
                itemList.RefreshItems();

                // 恢复上次选中
                if (selectedIndexDict.TryGetValue(configSelection.value, out int savedIndex))
                {
                    if (savedIndex >= 0 && savedIndex < soList.Count)
                    {
                        itemList.AddToSelection(savedIndex);
                        itemList.ScrollToItem(savedIndex);
                        CreateAssetsPanel(soList[savedIndex]);
                    }
                }
            }
        }

        private void ChangeCfg(ChangeEvent<string> value)
        {
            content.Clear();
            contentName.text = "配置名称";
            itemList.ClearSelection();
            groupList.ClearSelection();
            LoadAllAssets(value.newValue + "CfgSO");
        }

        private void SaveSort()
        {
            if (string.IsNullOrEmpty(configSelection.value)) return;

            var sortDict = EditorPrefs.HasKey(SortKey)
                ? JsonMapper.ToObject<Dictionary<string, List<string>>>(EditorPrefs.GetString(SortKey))
                : new Dictionary<string, List<string>>();

            sortDict[configSelection.value] = soList
                .Where(so => so.name != nameof(AbilityNull))
                .Select(so => so.name)
                .ToList();

            EditorPrefs.SetString(SortKey, JsonMapper.ToJson(sortDict));
        }



        private void FindFile()
        {
            string searchText = filePath.value;
            if (string.IsNullOrEmpty(searchText))
            {
                EditorUtility.DisplayDialog("错误", "请输入搜索内容！", "确定");
                return;
            }

            var matches = groupDict.Values
                .SelectMany(list => list)
                .Where(so => so.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (matches.Count == 0)
            {
                EditorUtility.DisplayDialog("未找到配置", $"未找到匹配“{searchText}”的配置", "确定");
                return;
            }
            else if (matches.Count == 1)
            {
                SelectSO(matches[0]);
            }
            else
            {
                SearchResultWindow.ShowWindow(matches, SelectSO);
            }
        }

        private void SelectSO(CustomScriptableObject so)
        {
            if (so == null || so.name == nameof(AbilityNull)) return;

            foreach (var kv in groupDict)
            {
                if (kv.Value.Contains(so))
                {
                    int groupIndex = groupNames.FindIndex(g => g.key == kv.Key);
                    if (groupIndex >= 0)
                    {
                        groupList.SetSelection(groupIndex);
                        int itemIndex = kv.Value.IndexOf(so);
                        itemList.ClearSelection();
                        itemList.AddToSelection(itemIndex);
                        itemList.ScrollToItem(itemIndex);
                        CreateAssetsPanel(so);
                        FocusAsset(so);
                    }
                    break;
                }
            }
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
            var className = configSelection.value + "CfgSO";
            var cfg = UnityEditorGlobals.Create(className, "New" + className);
            if (cfg != null)
            {
                LoadAllAssets(className);
                SelectSO(cfg as CustomScriptableObject);
            }

        }

        private void RemoveAsset()
        {
            var selectedItems = itemList.selectedItems?.Cast<CustomScriptableObject>().Where(so => so.name != nameof(AbilityNull)).ToList();
            if (selectedItems == null || selectedItems.Count == 0)
            {
                EditorUtility.DisplayDialog("删除提示", "没有选择任何可删除配置！", "确定");
                return;
            }

            string message = selectedItems.Count == 1 ?
                $"确定要删除配置【{selectedItems[0].name}】吗？" :
                $"确定要删除选中的 {selectedItems.Count} 个配置吗？";

            bool confirm = EditorUtility.DisplayDialog("删除确认", message, "确定", "取消");
            if (confirm)
            {
                DeleteSelectedAssets(selectedItems);
            }
        }

        private void DeleteSelectedAssets(List<CustomScriptableObject> items)
        {
            content.Clear();
            contentName.text = "配置名称";

            foreach (var obj in items)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                soList.Remove(obj);

                foreach (var kv in groupDict)
                {
                    if (kv.Value.Contains(obj))
                    {
                        kv.Value.Remove(obj);
                        break;
                    }
                }

                AssetDatabase.DeleteAsset(path);
            }

            // 清理空分组
            var emptyGroups = groupDict.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList();
            foreach (var g in emptyGroups) groupDict.Remove(g);

            groupNames = groupDict.Select(kv => (kv.Key, Path.GetFileName(kv.Key))).ToList();
            groupList.itemsSource = groupNames;
            groupList.RefreshItems();

            if (groupNames.Count > 0)
            {
                // 选中第一个分组
                var firstKey = groupNames[0].key;
                groupList.SetSelection(0);

                if (groupDict.TryGetValue(firstKey, out var list) && list.Count > 0)
                {
                    soList = list;
                    itemList.itemsSource = soList;
                    itemList.RefreshItems();

                    // 自动选中第一个资产
                    itemList.ClearSelection();
                    itemList.AddToSelection(0);
                    itemList.ScrollToItem(0);
                    CreateAssetsPanel(soList[0]);
                }
                else
                {
                    soList.Clear();
                    itemList.itemsSource = soList;
                    itemList.RefreshItems();
                }
            }
            else
            {
                soList.Clear();
                itemList.itemsSource = soList;
                itemList.RefreshItems();
            }

            AssetDatabase.Refresh();
        }

        private void CopyName(CustomScriptableObject so)
        {
            if (so == null) return;
            GUIUtility.systemCopyBuffer = so.name;
            UCMDebug.Log($"已复制名称: {so.name}");
        }

        private void Rename(CustomScriptableObject so, int index)
        {
            if (so == null) return;
            RenameWindow.ShowPanel(so.name, newName =>
            {
                if (string.IsNullOrEmpty(newName) || soList.Any(x => x.name == newName))
                {
                    EditorUtility.DisplayDialog("错误", "重复或无效名称！", "确定");
                    return;
                }

                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(so), newName);
                so.name = newName;
                itemList.RefreshItem(index);
                AssetDatabase.SaveAssets();
            });
        }

        private void FocusAsset(CustomScriptableObject so)
        {
            if (so == null) return;
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<CustomScriptableObject>(AssetDatabase.GetAssetPath(so));
            EditorUtility.FocusProjectWindow();
        }



        private void ExportJson()
        {
            if (itemList.selectedItems?.Count() < 1)
            {
                EditorUtility.DisplayDialog("错误", "没有选择配置！", "确定");
                return;
            }

            if (exportActions.TryGetValue(configSelection.value, out var action))
            {
                action(false);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void ExportCurrentCfgAllJson()
        {
            if (soList.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "没有可导出的项目！", "确定");
                return;
            }

            if (exportActions.TryGetValue(configSelection.value, out var action))
            {
                action(true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void ImportJson()
        {
            string path = EditorUtility.OpenFilePanel("选择文件", "", "json");
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("错误", "导入取消或内容无效！", "确定");
                return;
            }

            fileContent = File.ReadAllText(path);

            if (importActions.TryGetValue(configSelection.value, out var action))
            {
                action();
                LoadAllAssets($"{configSelection.value}{nameCfg}");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("提示", "导入完成", "确定");
            }
        }

        private void SaveJson(string json)
        {
            string filePath = GetSaveFilePath();
            if (!string.IsNullOrEmpty(filePath))
            {
                File.WriteAllText(filePath, json);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("提示", "JSON文件已保存到: " + filePath, "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("警告", "保存取消或内容无效！", "确定");
            }
        }

        private void Write<T, T2>(bool isAll = false, string name = "cfg")
            where T : CustomScriptableObject
            where T2 : class
        {
            SaveJson(JsonMapper.ToJson(
                (isAll ? soList.OfType<T>() : itemList.selectedItems.OfType<T>())
                .ToDictionary(
                    item => item.CachedSoName,
                    item =>
                    {
                        var type = item.GetType();
                        var field = type.GetField(name, flags);
                        if (field != null)
                        {
                            return field.GetValue(item) as T2;
                        }

                        var property = type.GetProperty(name, flags);
                        if (property != null)
                        {
                            return property.GetValue(item) as T2;
                        }

                        return null; // 如果没有找到字段或属性
                    }
                )
            ));
        }

        private void Read<T, Y>() where Y : ScriptableObject
        {
            var cfg = JsonMapper.ToObject<Dictionary<string, T>>(fileContent);
            foreach (var item in cfg)
            {
                if (string.IsNullOrWhiteSpace(item.Key)) continue;

                var data = soList.Find(x => x.name == item.Key) as Y;
                if (data != null)
                {
                    SetCfgValue(data, item.Value);
                    EditorUtility.SetDirty(data);
                    UCMDebug.Log($"已更新{typeof(Y).Name}配置【{item.Key}】数据");
                }
                else
                {
                    string[] guids = AssetDatabase.FindAssets(item.Key + " t:" + typeof(Y).Name, null);
                    Y so = null;
                    foreach (string guid in guids)
                    {
                        Y asset = AssetDatabase.LoadAssetAtPath<Y>(AssetDatabase.GUIDToAssetPath(guid));
                        if (asset != null && asset.name == item.Key)
                        {
                            so = asset;
                            break;
                        }
                    }
                    // 如果没找到，创建新的
                    if (so == null) so = UnityEditorGlobals.Create<Y>(item.Key);
                    SetCfgValue(so, item.Value);
                    EditorUtility.SetDirty(so);
                    UCMDebug.Log($"创建新{typeof(Y).Name}配置【{item.Key}】数据");
                }
            }
        }


        private void SetCfgValue<Y, T>(Y obj, T value) where Y : ScriptableObject
        {
            Type type = typeof(Y);
            FieldInfo fieldInfo = type.GetField("cfg", flags);

            if (fieldInfo != null)
            {
                fieldInfo.SetValue(obj, value);
            }
            else
            {
                PropertyInfo propertyInfo = type.GetProperty("cfg", flags);
                propertyInfo?.SetValue(obj, value);
            }
        }

        private string GetSaveFilePath()
        {
            if (Directory.Exists(jsonPath.value))
            {
                return EditorUtility.SaveFilePanel(
                    "保存Json文件",
                    jsonPath.value,
                    configSelection.value,
                    "json"
                );
            }
            else
            {
                return EditorUtility.SaveFilePanelInProject(
                    "保存Json文件",
                    configSelection.value,
                    "json",
                    "请输入文件名以保存JSON数据"
                );
            }
        }

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

            // 滚动视图
            scrollView = new ScrollView();
            rootVisualElement.Add(scrollView);

            RefreshResults();
        }

        private void RefreshResults()
        {
            scrollView.Clear();

            if (results == null || results.Count == 0)
            {
                Label emptyLabel = new("无匹配项");
                scrollView.Add(emptyLabel);
                return;
            }

            foreach (var so in results)
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

        // 如果需要动态更新搜索结果，可以暴露一个方法
        public void UpdateResults(List<CustomScriptableObject> newResults)
        {
            results = newResults;
            RefreshResults();
        }
    }


}
#endif

