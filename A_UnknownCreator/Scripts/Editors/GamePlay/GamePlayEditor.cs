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
        //�����б�
        private ListView itemList;
        //��ǰ����
        private ScrollView content;
        //��ǰ�������ƣ��Ƴ��ʲ�����
        private Label contentName, removeAssetName;
        //����ѡ����ѡ��
        private DropdownField configSelection;
        //������ȷ��ɾ������
        private TemplateContainer popup;
        private TextField jsonPath;

        //��ǰ�����б�
        private static List<CustomScriptableObject> soList = new();
        //��¼ѡ�������
        private static Dictionary<string, CustomScriptableObject> soDict = new();
        //��¼����GUID
        private static Dictionary<string, string> guidDict = new();

        private Dictionary<string, Action<bool>> exportActions;
        private Dictionary<string, Action> importActions;
        private const string nameCfg = "CfgSO";    //����β����ȷ���ʲ�����β��һ�£�
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
                    removeAssetName.text = "�Ƴ��ʲ�����" + result;
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
                contentName.text = "��������";
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

            //����ѡ��
            configSelection.RegisterValueChangedCallback((value) =>
            {
                content.Clear();
                contentName.text = "��������";
                itemList.ClearSelection();
                if (soDict.TryGetValue(value.newValue, out var result))
                    CreateAssetsPanel(result);
                LoadAllAssets(value.newValue + nameCfg);
            });

            //�ʲ��б�
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
            UCMDebug.Log($"�Ѹ�������: {GUIUtility.systemCopyBuffer}");
        }

        private void Rename(CustomScriptableObject so, int index)
        {
            RenameWindow.Show(so.name, newName =>
            {
                if (string.IsNullOrEmpty(newName)) return;

                if (soList.Exists(x => x.name == newName))
                {
                    UCMDebug.LogWarning($"�ظ�����,�޷��޸�>>>{newName}");
                    return;
                }

                var path = AssetDatabase.GetAssetPath(so);
                AssetDatabase.RenameAsset(path, newName);

                if (so is StatsCfgSO stats)
                    stats.cfg.idName = newName;

                itemList.RefreshItem(index);
                AssetDatabase.SaveAssets();
                UCMDebug.Log($"�������޸�Ϊ: {newName}");
            });
        }

        private void FocusAsset(CustomScriptableObject so)
        {
            var path = AssetDatabase.GetAssetPath(so);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<CustomScriptableObject>(path);
            UCMDebug.Log($"��ѡ���ʲ�: {so.name}��·��: {path}");
        }



        #region JOSN

        private void ExportJson()
        {
            if (itemList.selectedItems?.Count() < 1)
            {
                UCMDebug.LogWarning("û��ѡ������");
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
                UCMDebug.LogWarning("û�пɵ�������Ŀ");
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

            string path = EditorUtility.OpenFilePanel("ѡ���ļ�", "", "json");
            if (string.IsNullOrEmpty(path))
            {
                UCMDebug.LogWarning("����ʧ��,������Ч");
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
                UCMDebug.Log("JSON�ļ��ѱ��浽: " + filePath);
            }
            else
            {
                UCMDebug.LogWarning("����ȡ����·����Ч��");
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

                        return null; // ���û���ҵ��ֶλ�����
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
                    UCMDebug.Log($"�Ѹ���{typeof(Y).Name}���á�{item.Key}������");
                }
                else
                {
                    var so = GetOrCreateStatsCfg<Y>(item.Key);
                    SetCfgValue(so, item.Value);
                    EditorUtility.SetDirty(so);
                    UCMDebug.Log($"������{typeof(Y).Name}���á�{item.Key}������");
                }
            }
        }


        private void FindJsonPath()
        {
            string folderPath = EditorUtility.OpenFolderPanel("ѡ��һ���ļ���", "", "");
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
                    "����Json�ļ�",
                    jsonPath.value,
                    configSelection.value,
                    "json"
                );
            }
            else
            {
                return EditorUtility.SaveFilePanelInProject(
                    "����Json�ļ�",
                    configSelection.value,
                    "json",
                    "�������ļ����Ա���JSON����"
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
            var window = GetWindow<RenameWindow>("�޸�����");
            window.CreateUI(currentName, renameCallback);
        }

        private void CreateUI(string currentName, System.Action<string> renameCallback)
        {
            rootVisualElement.Clear();

            rootVisualElement.Add(new TextField("�����µ�����:")
            {
                value = currentName
            });

            rootVisualElement.Add(new Button(() =>
            {
                renameCallback?.Invoke(nameField.value);
                Close();
            })
            {
                text = "ȷ��"
            });

            rootVisualElement.Add(new Button(() => Close())
            {
                text = "ȡ��"
            });
        }
    }
}
#endif