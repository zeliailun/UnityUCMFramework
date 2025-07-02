#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
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
        //  [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset;
        private VisualElement root => rootVisualElement;
        //配置列表
        private ListView itemList;
        //当前配置
        private ScrollView content;
        //当前配置名称，移除资产名称
        private Label contentName, removeAssetName;
        //配置选择，组选择
        private DropdownField configSelection;
        //弹出的确认删除窗口
        private TemplateContainer popup;
        private TextField jsonPath;

        //当前配置列表
        private static List<CustomScriptableObject> soList = new();
        //记录选择的配置
        private static Dictionary<string, CustomScriptableObject> soDict = new();
        //记录配置GUID
        private static Dictionary<string, string> guidDict = new();

        private Dictionary<string, Action<bool>> exportActions;
        private Dictionary<string, Action> importActions;
        private const string nameCfg = "CfgSO";    //配置尾名（确保资产名称尾部一致）
        private const string visualTreeName = "GamePlayEditor";
        private string fileContent;
        private string folderPathKey = nameof(folderPathKey);
        private BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;


        [MenuItem("UnknownCreator/GamePlayEditor")]
        public static void GamePlay()
        {
            GamePlayEditor wnd = GetWindow<GamePlayEditor>();
            wnd.titleContent = new GUIContent("GamePlayEditor");
            wnd.Show(true);
        }

        private void OnInspectorUpdate()
        {
            /* if (itemList?.selectedItem != null)
                 itemList.RefreshItem(itemList.selectedIndex);*/
        }


        public void CreateGUI()
        {
            exportActions = new()
            {
                { nameof(CfgTypes.UnitModel), b => Write<UnitModelCfgSO,UnitModelCfg>(b) },
                { nameof(CfgTypes.StatsGroup), b => Write<StatsGroupCfgSO,List<OverrideStats>>(b) },
                { nameof(CfgTypes.Stats), b=> Write<StatsCfgSO,StatsCfg>(b) },
                { nameof(CfgTypes.Sound), b => Write<SoundCfgSO,SoundCfg>(b) },
                { nameof(CfgTypes.Anim), b => Write<AnimCfgSO,List<AnimCfgInfo>>(b) },
                { nameof(CfgTypes.Ability), b=> Write<AbilityCfgSO,AbilityCfg>(b) },
                { nameof(CfgTypes.Unit),b => Write<UnitCfgSO,UnitCfg>(b) }
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
                m_VisualTreeAsset = EditorUtils.GetAsset<VisualTreeAsset>(visualTreeName);

            root.Add(m_VisualTreeAsset.CloneTree());

            popup = root.Q<TemplateContainer>("Popup");
            removeAssetName = root.Q<Label>("RemoveAssetName");
            content = root.Q<ScrollView>("Content");
            contentName = root.Q<Label>("ContentName");
            itemList = root.Q<ListView>("ItemList");
            jsonPath = root.Q<TextField>("JsonPath");

            if (EditorPrefs.HasKey(folderPathKey))
                jsonPath.value = EditorPrefs.GetString(folderPathKey);

            var addAsset = root.Q<Button>("AddAsset");
            addAsset.clicked += () =>
            {
                var className = configSelection.value + nameCfg;
                EditorUtils.Create(className, "New" + className);
                LoadAllAssets(className);
            };

            var removeAsset = root.Q<Button>("RemoveAsset");
            removeAsset.clicked += () =>
            {
                var result = itemList.selectedItems?.Count();
                if (result is null || result < 1) return;
                if (result == 1)
                    removeAssetName.text = ((ScriptableObject)itemList.selectedItem).name;
                else
                    removeAssetName.text = "移除资产数量" + result;
                popup.style.display = DisplayStyle.Flex;
            };

            var findPath = root.Q<Button>("FindPath");
            findPath.clicked += FindJsonPath;

            var export = root.Q<Button>("Export");
            export.clicked += ExportJson;

            var import = root.Q<Button>("Import");
            import.clicked += ImportJson;

            var allExport = root.Q<Button>("AllExport");
            allExport.clicked += ExportCurrentCfgAllJson;

            var copyName = root.Q<Button>("CopyName");
            copyName.clicked += () => { CopyName(itemList.selectedItem as CustomScriptableObject); };

            var rename = root.Q<Button>("Rename");
            rename.clicked += () => { Rename(itemList.selectedItem as CustomScriptableObject, itemList.selectedIndex); };


            var focusAsset = root.Q<Button>("FocusAsset");
            focusAsset.clicked += () => { FocusAsset(itemList.selectedItem as CustomScriptableObject); };


            var ok = root.Q<Button>("OK");
            ok.clicked += () =>
            {
                var result = itemList.selectedItems?.Count();
                if (result is null || result < 1) return;
                content.Clear();
                contentName.text = "配置名称";
                foreach (var item in itemList.selectedItems)
                {
                    var obj = (CustomScriptableObject)item;
                    soDict.Remove(configSelection.text);
                    soList.Remove(obj);
                    guidDict.Remove(obj.name);
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(obj));
                }
                popup.style.display = DisplayStyle.None;
                AssetDatabase.Refresh();
                itemList.RefreshItems();
            };

            var no = root.Q<Button>("NO");
            no.clicked += () =>
            {
                popup.style.display = DisplayStyle.None;
            };


            configSelection = root.Q<DropdownField>("ConfigSelection");
            configSelection.index = 0;
            configSelection.choices = Enum.GetValues(typeof(CfgTypes)).Cast<CfgTypes>().Select(e => e.ToString()).ToList();
            configSelection.value = configSelection.choices[0];

            LoadAllAssets(configSelection.value + nameCfg);

            //配置选择
            configSelection.RegisterValueChangedCallback((value) =>
            {
                content.Clear();
                contentName.text = "配置名称";
                itemList.ClearSelection();
                if (soDict.TryGetValue(value.newValue, out var result))
                    CreateAssetsPanel(result);
                LoadAllAssets(value.newValue + nameCfg);
            });

            //资产列表
            itemList.itemsSource = soList;
            itemList.selectionType = SelectionType.Multiple;
            itemList.selectionChanged += selectedItems => CreateAssetsPanel(selectedItems.FirstOrDefault() as CustomScriptableObject);
            itemList.bindItem = (e, i) =>
            {
                var currentSO = soList[i];

                var icon = e.Q<VisualElement>("Icon");
                var nameLabel = e.Q<Label>("Name");

                if (currentSO is AbilityCfgSO abilitySO)
                    icon.style.backgroundImage = abilitySO.icon;
                else
                    icon.style.backgroundImage = null;

                if (nameLabel.text != currentSO.name)
                    nameLabel.text = currentSO.name;

                if(currentSO.name=="AbilityNull")
                {
                    e.Q<VisualElement>("Hide").style.display = DisplayStyle.Flex;
                }else{
                    e.Q<VisualElement>("Hide").style.display = DisplayStyle.None;
                }

            };
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

        private T GetOrCreateStatsCfg<T>(string key) where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets(key + " t:" + typeof(T).Name, null);
            T so;
            foreach (string guid in guids)
            {
                so = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (so != null && so.name == key) return so;
            }
            return EditorUtils.Create<T>(key);
        }

        private void CreateAssetsPanel(CustomScriptableObject activeItem)
        {
            if (activeItem == null ||
                activeItem.cfgName == nameof(AbilityNull)) return;


            content.Clear();
            contentName.text = activeItem.name;

            var editor = UnityEditor.Editor.CreateEditor(activeItem);
            content.Add(new IMGUIContainer()
            {
                onGUIHandler = editor.OnInspectorGUI
            });

            if (!soDict.TryGetValue(configSelection.text, out var value))
                soDict.Add(configSelection.text, activeItem);
            else
                soDict[configSelection.text] = activeItem;
        }

        private void LoadAllAssets(string name)
        {
            soList.Clear();
            string[] guids = AssetDatabase.FindAssets("t:" + name, null);
            string path;
            CustomScriptableObject so;
            foreach (string guid in guids)
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                so = AssetDatabase.LoadAssetAtPath<CustomScriptableObject>(path);
                soList.Add(so);
                guidDict.TryAdd(so.name, guid);
            }
            itemList.RefreshItems();
        }



        private void CopyName(CustomScriptableObject so)
        {
            GUIUtility.systemCopyBuffer = so.name;
            UCMDebug.Log($"已复制名称: {GUIUtility.systemCopyBuffer}");
        }

        private void Rename(CustomScriptableObject so, int index)
        {
            RenameWindow.Show(so.name, newName =>
            {
                if (string.IsNullOrEmpty(newName)) return;

                if (soList.Exists(x => x.name == newName))
                {
                    UCMDebug.LogWarning($"重复名称,无法修改>>>{newName}");
                    return;
                }

                var path = AssetDatabase.GetAssetPath(so);
                AssetDatabase.RenameAsset(path, newName);

                if (so is StatsCfgSO stats)
                    stats.cfg.idName = newName;

                itemList.RefreshItem(index);
                AssetDatabase.SaveAssets();
                UCMDebug.Log($"名称已修改为: {newName}");
            });
        }

        private void FocusAsset(CustomScriptableObject so)
        {
            var path = AssetDatabase.GetAssetPath(so);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<CustomScriptableObject>(path);
            UCMDebug.Log($"已选择资产: {so.name}，路径: {path}");
        }



        #region JOSN

        private void ExportJson()
        {
            if (itemList.selectedItems?.Count() < 1)
            {
                UCMDebug.LogWarning("没有选择配置");
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
                UCMDebug.LogWarning("没有可导出的项目");
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
                UCMDebug.LogWarning("导入失败,内容无效");
                return;
            }

            fileContent = File.ReadAllText(path);

            if (importActions.TryGetValue(configSelection.value, out var action))
            {
                action();
                LoadAllAssets($"{configSelection.value}{nameCfg}");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
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
                UCMDebug.Log("JSON文件已保存到: " + filePath);
            }
            else
            {
                UCMDebug.LogWarning("保存取消或路径无效。");
            }
        }

        private void Write<T, T2>(bool isAll = false, string name = "cfg")
        where T : CustomScriptableObject
        where T2 : class
        {
            SaveJson(JsonMapper.ToJson(
                (isAll ? soList.OfType<T>() : itemList.selectedItems.OfType<T>())
                .ToDictionary(
                    item => item.cfgName,
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
                    })));
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
                    var so = GetOrCreateStatsCfg<Y>(item.Key);
                    SetCfgValue(so, item.Value);
                    EditorUtility.SetDirty(so);
                    UCMDebug.Log($"创建新{typeof(Y).Name}配置【{item.Key}】数据");
                }
            }
        }


        private void FindJsonPath()
        {
            string folderPath = EditorUtility.OpenFolderPanel("选择一个文件夹", "", "");
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                jsonPath.value = folderPath;
                EditorPrefs.SetString(folderPathKey, jsonPath.value);
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

        #endregion

    }


    public class RenameWindow : EditorWindow
    {
        private TextField nameField;
        private System.Action<string> onRenameConfirmed;

        public static void Show(string currentName, System.Action<string> renameCallback)
        {
            var window = GetWindow<RenameWindow>("修改名称");
            window.CreateUI(currentName, renameCallback);
        }

        private void CreateUI(string currentName, System.Action<string> renameCallback)
        {
            rootVisualElement.Clear();

            rootVisualElement.Add(new TextField("输入新的名称:")
            {
                value = currentName
            });

            rootVisualElement.Add(new Button(() =>
            {
                renameCallback?.Invoke(nameField.value);
                Close();
            })
            {
                text = "确认"
            });

            rootVisualElement.Add(new Button(() => Close())
            {
                text = "取消"
            });
        }
    }
}
#endif