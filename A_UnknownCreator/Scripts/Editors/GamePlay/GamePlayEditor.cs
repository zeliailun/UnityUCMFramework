#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
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

        private static Dictionary<string, Action<bool>> exportActions;
        private static Dictionary<string, Action> importActions;

        //当前列表
        private static List<CustomScriptableObject> soList = new();

        //列表组
        private static Dictionary<string, List<CustomScriptableObject>> groupDict = new();

        //组名称
        private static List<(string key, string displayName)> groupNames = new();

        // 保存每个配置类型的选中索引
        private static Dictionary<string, int> selectedIndexDict = new();

        private static string fileContent;
        private const string nameCfg = "CfgSO"; //配置尾名（确保资产名称尾部一致）
        private const string SortKey = nameof(SortKey);
        private const string folderPathKey = nameof(folderPathKey);
        private const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private const string LastCfgType = nameof(LastCfgType);
        private const string LastGroupIndex = nameof(LastGroupIndex);
        private const string LastItemIndex = nameof(LastItemIndex);


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
            exportActions ??= new()
            {
                { nameof(CfgTypes.UnitModel), b => Write<UnitModelCfgSO, UnitModelCfg>(b) },
                { nameof(CfgTypes.StatsGroup), b => Write<StatsGroupCfgSO, List<OverrideStats>>(b) },
                { nameof(CfgTypes.Stats), b => Write<StatsCfgSO, StatsCfg>(b) },
                { nameof(CfgTypes.Sound), b => Write<SoundCfgSO, SoundCfg>(b) },
                { nameof(CfgTypes.Anim), b => Write<AnimCfgSO, List<AnimCfgInfo>>(b) },
                { nameof(CfgTypes.Ability), b => Write<AbilityCfgSO, AbilityCfg>(b) },
                { nameof(CfgTypes.Unit), b => Write<UnitCfgSO, UnitCfg>(b) }
            };

            importActions ??= new Dictionary<string, Action>
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

            openScript = root.Q<Button>("OpenScript");
            openScript.clicked += OpenScript;

            root.Q<Button>("FindFile").clicked += FindFile;
            root.Q<Button>("FindJsonPath").clicked += FindJsonPath;
            root.Q<Button>("AddAsset").clicked += AddAsset;
            root.Q<Button>("RemoveAsset").clicked += RemoveAsset;
            root.Q<Button>("Export").clicked += () => ExportJson(false);
            root.Q<Button>("AllExport").clicked += () => ExportJson(true);
            root.Q<Button>("Import").clicked += ImportJson;
            root.Q<Button>("CopyName").clicked += () => CopyName(itemList.selectedItem as CustomScriptableObject);
            root.Q<Button>("Rename").clicked += () => Rename(itemList.selectedItem as CustomScriptableObject, itemList.selectedIndex);
            root.Q<Button>("FocusAsset").clicked += () => FocusAsset(itemList.selectedItem as CustomScriptableObject);



            configSelection.choices = new List<string>(Enum.GetNames(typeof(CfgTypes)));
            string lastCfgType = EditorPrefs.GetString(LastCfgType, null);
            if (!string.IsNullOrEmpty(lastCfgType) && configSelection.choices.Contains(lastCfgType))
                configSelection.value = lastCfgType;
            else
                configSelection.value = configSelection.choices[0];
            configSelection.RegisterValueChangedCallback(ChangeCfg);


            groupList.selectionType = SelectionType.Single;
            groupList.makeItem = () => new Label();
            groupList.bindItem = (element, index) =>
            {
                (element as Label).text = groupNames[index].displayName;
            };
            groupList.selectionChanged += ChangeGroup;


            itemList.selectionType = SelectionType.Multiple;
            itemList.makeItem = CreateItemElement;
            itemList.bindItem = BindItemElement;
            itemList.selectionChanged += OnSelectionChanged;
            itemList.itemIndexChanged += (oldIndex, newIndex) => SaveSort();

            LoadAllAssets(configSelection.value + "CfgSO");
        }




        private void CreateAssetsPanel(CustomScriptableObject activeItem)
        {
            if (activeItem == null) return;
            content.Clear();
            contentName.text = activeItem.name;
            var inspector = new InspectorElement();
            content.Add(inspector);
            inspector.Bind(new SerializedObject(activeItem));

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

            // 设置 groupList 数据
            groupList.itemsSource = groupNames;
            groupList.RefreshItems();

            // 恢复上次选中的分组
            int savedGroupIndex = EditorPrefs.GetInt(LastGroupIndex, 0);
            if (groupNames.Count == 0) return;

            if (savedGroupIndex < 0 || savedGroupIndex >= groupNames.Count)
                savedGroupIndex = 0;

            groupList.SetSelection(savedGroupIndex);
            string groupKeyToSelect = groupNames[savedGroupIndex].key;

            if (!groupDict.TryGetValue(groupKeyToSelect, out soList))
                soList = new List<CustomScriptableObject>();

            // 设置 itemList 数据
            itemList.itemsSource = soList;
            itemList.RefreshItems();

            // 恢复上次选中的项目
            int savedItemIndex = EditorPrefs.GetInt(LastItemIndex, 0);
            if (soList.Count > 0)
            {
                if (savedItemIndex < 0 || savedItemIndex >= soList.Count)
                    savedItemIndex = 0;

                itemList.ClearSelection();
                itemList.AddToSelection(savedItemIndex);
                itemList.ScrollToItem(savedItemIndex);
                CreateAssetsPanel(soList[savedItemIndex]);
            }
        }

        private void ChangeGroup(IEnumerable<object> selectedItems)
        {
            if (!selectedItems.Any()) return;

            var selectedTuple = (ValueTuple<string, string>)selectedItems.First();
            string groupKey = selectedTuple.Item1;

            if (!groupDict.TryGetValue(groupKey, out var list)) return;

            // 切换分组
            soList = list;
            itemList.itemsSource = soList;
            itemList.ClearSelection();
            itemList.RefreshItems();

            // 恢复上次选中的 item 索引
            if (selectedIndexDict.TryGetValue(configSelection.value, out int savedIndex) &&
                savedIndex >= 0 && savedIndex < soList.Count)
            {
                itemList.AddToSelection(savedIndex);
                itemList.ScrollToItem(savedIndex);
                CreateAssetsPanel(soList[savedIndex]);
                EditorPrefs.SetInt(LastItemIndex, savedIndex);
            }

            // 保存当前分组索引
            int currentGroupIndex = groupList?.selectedIndex ?? -1;
            EditorPrefs.SetInt(LastGroupIndex, currentGroupIndex);
        }

        private void ChangeCfg(ChangeEvent<string> value)
        {
            EditorPrefs.SetString(LastCfgType, value.newValue);
            content.Clear();
            contentName.text = "配置名称";
            itemList.ClearSelection();
            groupList.ClearSelection();
            LoadAllAssets(value.newValue + "CfgSO");

            if (value.newValue == CfgTypes.Ability.ToString())
                openScript.style.display = DisplayStyle.Flex;
            else
                openScript.style.display = DisplayStyle.None;
        }

        private VisualElement CreateItemElement()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;

            // 图标
            var icon = new VisualElement { name = "Icon", style = { width = 24, height = 24, marginTop = 7 } };
            container.Add(icon);

            // 名称
            var nameLabel = new Label { name = "Name", style = { marginTop = 7 } };
            container.Add(nameLabel);

            // 隐藏标记
            var hideMark = new VisualElement { name = "Hide" };
            container.Add(hideMark);

            return container;
        }

        private void BindItemElement(VisualElement e, int i)
        {
            if (!soList.IsValid() || i >= soList.Count) return;

            var currentSO = soList[i];

            var icon = e.Q<VisualElement>("Icon");
            var nameLabel = e.Q<Label>("Name");
            var hideMark = e.Q<VisualElement>("Hide");

            // 设置图标
            icon.style.backgroundImage = currentSO is AbilityCfgSO abilitySO ? abilitySO.icon?.editorAsset : null;

            // 设置名称
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

        private void OnSelectionChanged(IEnumerable<object> items)
        {
            var first = items.FirstOrDefault() as CustomScriptableObject;
            if (first == null || first.name == nameof(AbilityNull))
            {
                itemList.ClearSelection();
                return;
            }

            int index = soList.IndexOf(first);
            if (index >= 0)
                EditorPrefs.SetInt(LastItemIndex, index); // 保存选中索引

            CreateAssetsPanel(first);
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

        private void SelectSO(CustomScriptableObject so)
        {
            if (so == null || so.name == nameof(AbilityNull)) return;

            foreach (var kv in groupDict)
            {
                if (kv.Value.Contains(so))
                {
                    // 保存分组索引
                    int groupIndex = groupNames.FindIndex(g => g.key == kv.Key);
                    if (groupIndex >= 0)
                    {
                        groupList.SetSelection(groupIndex);
                        // 保存项目索引
                        int itemIndex = kv.Value.IndexOf(so);
                        itemList.ClearSelection();
                        itemList.AddToSelection(itemIndex);
                        itemList.ScrollToItem(itemIndex);
                        EditorPrefs.SetInt(LastItemIndex, itemIndex);

                        CreateAssetsPanel(so);
                        FocusAsset(so);
                    }
                    break;
                }
            }
        }




        #region 功能栏

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

                // 如果是 Ability 配置，额外创建脚本
                if (configSelection.value == nameof(CfgTypes.Ability))
                {

                    string abilityName = cfg.name;
                    string absolutePath = EditorUtility.OpenFolderPanel("选择脚本存放目录", Application.dataPath, "");
                    if (string.IsNullOrEmpty(absolutePath)) return;

                    if (!absolutePath.StartsWith(Application.dataPath))
                    {
                        EditorUtility.DisplayDialog("错误", "必须选择在 Assets 内的文件夹！", "确定");
                        return;
                    }

                    string relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
                    string scriptPath = Path.Combine(relativePath, $"{abilityName}.cs");

                    if (!File.Exists(scriptPath))
                    {
                        string scriptContent = $@"
using UnityEngine;
using UnknownCreator.Modules;

public class {abilityName} : AbilityBase
{{
        public override void OnCreated()
        {{

        }}

        protected override void OnRelease()
        {{

        }}  
}}";

                        File.WriteAllText(scriptPath, scriptContent);
                    }

                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("成功", $"已生成 Ability 脚本：{scriptPath}", "确定");

                }
            }

        }

        private void RemoveAsset()
        {
            // 获取可删除的选中项
            var selectedItems = itemList.selectedItems?.Cast<CustomScriptableObject>()
                .Where(so => so.name != nameof(AbilityNull)).ToList();

            if (selectedItems == null || selectedItems.Count == 0)
            {
                EditorUtility.DisplayDialog("删除提示", "没有选择任何可删除配置！", "确定");
                return;
            }

            // 删除确认
            string message = selectedItems.Count == 1 ?
                $"确定要删除配置【{selectedItems[0].name}】吗？" :
                $"确定要删除选中的 {selectedItems.Count} 个配置吗？";

            if (!EditorUtility.DisplayDialog("删除确认", message, "确定", "取消"))
                return;

            // 清空编辑面板
            content.Clear();
            contentName.text = "配置名称";

            // 删除资产
            foreach (var obj in selectedItems)
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

            AssetDatabase.Refresh();

            // 清理空分组
            var emptyGroups = groupDict.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key).ToList();
            foreach (var g in emptyGroups) groupDict.Remove(g);

            groupNames = groupDict.Select(kv => (kv.Key, Path.GetFileName(kv.Key))).ToList();
            groupList.itemsSource = groupNames;
            groupList.RefreshItems();

            // 处理当前分组和选中项
            if (groupNames.Count == 0)
            {
                soList.Clear();
                itemList.itemsSource = soList;
                itemList.RefreshItems();
                EditorPrefs.SetInt(LastItemIndex, -1);
                EditorPrefs.SetInt(LastGroupIndex, -1);
                return;
            }

            int currentGroupIndex = groupList.selectedIndex >= 0 ? groupList.selectedIndex : 0;
            if (currentGroupIndex >= groupNames.Count) currentGroupIndex = 0;

            string groupKey = groupNames[currentGroupIndex].key;
            if (!groupDict.TryGetValue(groupKey, out var list) || list.Count == 0)
            {
                var nonEmptyGroup = groupDict.FirstOrDefault(kv => kv.Value.Count > 0);
                if (nonEmptyGroup.Key != null)
                {
                    groupKey = nonEmptyGroup.Key;
                    currentGroupIndex = groupNames.FindIndex(g => g.key == groupKey);
                    list = nonEmptyGroup.Value;
                }
                else
                {
                    soList.Clear();
                    itemList.itemsSource = soList;
                    itemList.RefreshItems();
                    EditorPrefs.SetInt(LastItemIndex, -1);
                    EditorPrefs.SetInt(LastGroupIndex, -1);
                    return;
                }
            }

            // 更新分组和资产列表
            groupList.SetSelection(currentGroupIndex);
            EditorPrefs.SetInt(LastGroupIndex, currentGroupIndex);

            soList = list;
            itemList.itemsSource = soList;
            itemList.RefreshItems();

            // 自动选中第一个资产
            if (soList.Count > 0)
            {
                itemList.ClearSelection();
                itemList.AddToSelection(0);
                itemList.ScrollToItem(0);
                CreateAssetsPanel(soList[0]);
                EditorPrefs.SetInt(LastItemIndex, 0);
            }
            else
            {
                EditorPrefs.SetInt(LastItemIndex, -1);
            }
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
                contentName.text = newName;
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

        private void OpenScript()
        {
            var so = itemList.selectedItem as AbilityCfgSO;
            if (so == null) return;

            // 搜索项目中所有脚本文件（不限定类型）
            string[] guids = AssetDatabase.FindAssets("t:Script");
            string soName = so.cfgScript == null ? so.name : so.cfgScript.name;
            string targetPath = null;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                if (fileName == soName) // 精确匹配文件名
                {
                    targetPath = path;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(targetPath))
            {
                var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(targetPath);
                AssetDatabase.OpenAsset(scriptAsset);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", $"无法找到脚本 {soName}！", "确定");
            }
        }


        #endregion




        #region JSON

        private void ExportJson(bool isAll = false)
        {
            if (isAll)
            {
                if (groupDict.Count < 1)
                {
                    EditorUtility.DisplayDialog("错误", "没有选择配置！", "确定");
                    return;
                }
            }
            else
            {
                var items = itemList.selectedItems;
                if (items == null || !items.Any())
                {
                    EditorUtility.DisplayDialog("错误", "没有选择配置！", "确定");
                    return;
                }
            }


            if (exportActions.TryGetValue(configSelection.value, out var action))
            {
                action(isAll);
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

        private void Write<T, T2>(bool isAll = false, string name = "cfg")
         where T : CustomScriptableObject
         where T2 : class
        {
            var dict = new Dictionary<string, T2>();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            IEnumerable<T> items;

            if (isAll)
            {
                var temp = new List<T>();
                foreach (var list in groupDict.Values)
                    foreach (var item in list)
                        temp.Add(item as T);
                items = temp;
            }
            else
            {
                items = itemList.selectedItems.OfType<T>();
            }

            foreach (var item in items)
            {
                if (item == null) continue;

                object value = item.GetType().GetField(name, flags)?.GetValue(item)
                             ?? item.GetType().GetProperty(name, flags)?.GetValue(item);

                if (value is T2 typedValue)
                    dict[item.name] = typedValue;
            }

            string filePath = Directory.Exists(jsonPath.value)
                ? EditorUtility.SaveFilePanel("保存Json文件", jsonPath.value, configSelection.value, "json")
                : EditorUtility.SaveFilePanelInProject("保存Json文件", configSelection.value, "json", "请输入文件名以保存JSON数据");

            if (!string.IsNullOrEmpty(filePath))
            {
                File.WriteAllText(filePath, JsonMapper.ToJson(dict));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("提示", "JSON文件已保存到: " + filePath, "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("警告", "保存取消或内容无效！", "确定");
            }
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
                return;
            }

            PropertyInfo propertyInfo = type.GetProperty("cfg", flags);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(obj, value);
                return;
            }

            string message = $"配置【{obj.name}】类型 {type.FullName} 中不存在名为 'cfg' 的字段或属性！";
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

