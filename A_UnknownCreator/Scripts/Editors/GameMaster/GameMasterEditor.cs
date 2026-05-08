#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    public class GameMasterEditor : EditorWindow
    {
        private const string GroupName = "UCM";
        private const string VisualTreeName = "GameMasterEditor";
        private const string DebugSymbol = "UCMDebug";
        private const string GameCfgAddress = "UCM/GameCfg";
        private const string CfgKeyPrefix = "UnknownCreator.GameMasterEditor.SelectedGameCfg";

        private VisualTreeAsset m_VisualTreeAsset;
        private VisualElement root => rootVisualElement;
        private VisualElement initRoot;
        private Label tip;
        private TextField symbolInputText;
        private DropdownField symbolsDropdown;
        private ObjectField cfgSelector;
        private VisualElement mgrRoot;

        private GameCfgSO gameCfgSO;
        private string selectedCfgGuid;

        private static string CfgKey => $"{CfgKeyPrefix}.{Application.dataPath.GetHashCode()}";

        [MenuItem("UnknownCreator/GameMasterEditor")]
        public static void GM()
        {
            GameMasterEditor wnd = GetWindow<GameMasterEditor>();
            wnd.titleContent = new GUIContent("GameMasterEditor");

            Rect main = EditorGUIUtility.GetMainWindowPosition();
            Rect pos = wnd.position;
            pos.x = main.x + ((main.width - pos.width) * 0.5f);
            pos.y = main.y + ((main.height - pos.height) * 0.5f);
            wnd.position = pos;

            wnd.Show(true);
        }

        private void CreateGUI()
        {
            root.Clear();

            m_VisualTreeAsset = UnityEditorGlobals.GetAsset<VisualTreeAsset>(VisualTreeName);
            if (m_VisualTreeAsset == null)
            {
                ShowRootError($"找不到 UI 资源：{VisualTreeName}");
                return;
            }

            root.Add(m_VisualTreeAsset.CloneTree());

            if (!BindUI())
                return;

            RegisterUIEvents();
            RefreshSymbolsDropdown();
            LoadCfg();
            RefreshInspector();
            Init();
        }

        private bool BindUI()
        {
            initRoot = RequireElement<VisualElement>("InitRoot");
            tip = RequireElement<Label>("Tip");
            symbolInputText = RequireElement<TextField>("SymbolInputText");
            symbolsDropdown = RequireElement<DropdownField>("SymbolsList");
            cfgSelector = RequireElement<ObjectField>("GameCfg");
            mgrRoot = RequireElement<VisualElement>("Mgr");

            bool success = initRoot != null &&
                           tip != null &&
                           symbolInputText != null &&
                           symbolsDropdown != null &&
                           cfgSelector != null &&
                           mgrRoot != null;

            if (!success)
                ShowRootError("GameMasterEditor.uxml 缺少必要控件，请检查控件 Name 是否正确。");

            return success;
        }

        private T RequireElement<T>(string elementName) where T : VisualElement
        {
            T element = root.Q<T>(elementName);
            if (element == null)
                Debug.LogError($"GameMasterEditor 缺少 UI 控件：{elementName}");

            return element;
        }

        private void RegisterUIEvents()
        {
            Button addButton = RequireElement<Button>("AddSymbol");
            Button removeButton = RequireElement<Button>("RemoveSymbol");

            if (addButton != null)
                addButton.clicked += AddSymbol;

            if (removeButton != null)
                removeButton.clicked += RemoveSymbol;

            cfgSelector.objectType = typeof(GameCfgSO);
            cfgSelector.allowSceneObjects = false;
            cfgSelector.RegisterValueChangedCallback(evt =>
            {
                string previousGuid = selectedCfgGuid;

                gameCfgSO = evt.newValue as GameCfgSO;
                selectedCfgGuid = GetAssetGuid(gameCfgSO);

                RefreshInspector();
                SaveCfg();
                Init(previousGuid);
            });
        }

        private void ShowRootError(string message)
        {
            Debug.LogError(message);

            Label label = new Label(message);
            label.style.color = Color.red;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginLeft = 8;
            label.style.marginRight = 8;
            label.style.marginTop = 8;
            label.style.marginBottom = 8;
            root.Add(label);
        }

        private void SaveCfg()
        {
            if (!string.IsNullOrEmpty(selectedCfgGuid))
            {
                EditorPrefs.SetString(CfgKey, selectedCfgGuid);
            }
            else
            {
                EditorPrefs.DeleteKey(CfgKey);
            }
        }

        private void LoadCfg()
        {
            gameCfgSO = null;
            selectedCfgGuid = null;

            if (!EditorPrefs.HasKey(CfgKey))
            {
                cfgSelector.SetValueWithoutNotify(null);
                return;
            }

            string guid = EditorPrefs.GetString(CfgKey);
            string path = AssetDatabase.GUIDToAssetPath(guid);
            gameCfgSO = AssetDatabase.LoadAssetAtPath<GameCfgSO>(path);

            if (gameCfgSO == null)
            {
                EditorPrefs.DeleteKey(CfgKey);
                selectedCfgGuid = null;
                cfgSelector.SetValueWithoutNotify(null);
                return;
            }

            selectedCfgGuid = guid;
            cfgSelector.SetValueWithoutNotify(gameCfgSO);
        }

        private void RefreshInspector()
        {
            if (mgrRoot == null)
                return;

            mgrRoot.Clear();

            if (gameCfgSO != null)
                mgrRoot.Add(new InspectorElement(gameCfgSO));
        }

        private void Init(string obsoleteCfgGuid = null)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                SetInitState("Addressables 未初始化", Color.red);
                return;
            }

            AddressableAssetGroup group = settings.FindGroup(GroupName);

            if (gameCfgSO == null)
            {
                RemoveManagedEntries(settings, group, obsoleteCfgGuid, null);
                SetDefineSymbol(DebugSymbol, false);
                SetInitState("未初始化模块", Color.red);
                RefreshSymbolsDropdown();
                AssetDatabase.SaveAssets();
                return;
            }

            string cfgGuid = GetAssetGuid(gameCfgSO);
            if (string.IsNullOrEmpty(cfgGuid))
            {
                SetInitState("GameCfgSO 必须是项目资源 Asset", Color.red);
                return;
            }

            if (group == null)
            {
                List<AddressableAssetGroupSchema> schemasToCopy = settings.DefaultGroup != null
                    ? settings.DefaultGroup.Schemas
                    : new List<AddressableAssetGroupSchema>();

                group = settings.CreateGroup(GroupName, false, true, false, schemasToCopy);
            }

            if (group == null)
            {
                SetInitState($"创建 Addressables 分组失败：{GroupName}", Color.red);
                return;
            }

            // 只移除本工具管理的旧 GameCfg，不清空整个 UCM 分组，避免误删其它 Addressables 资源。
            RemoveManagedEntries(settings, group, obsoleteCfgGuid, cfgGuid);

            AddressableAssetEntry entry = group.GetAssetEntry(cfgGuid);
            if (entry == null)
                entry = settings.CreateOrMoveEntry(cfgGuid, group);

            if (entry == null)
            {
                SetInitState("创建 GameCfg Addressables Entry 失败", Color.red);
                return;
            }

            if (entry.address != GameCfgAddress)
                entry.address = GameCfgAddress;

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();

            SetDefineSymbol(DebugSymbol, true);
            RefreshSymbolsDropdown();
            SetInitState("已初始化模块", Color.green);
        }

        private static void RemoveManagedEntries(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string obsoleteGuid,
            string keepGuid)
        {
            if (settings == null || group == null)
                return;

            List<AddressableAssetEntry> entries = group.entries.ToList();
            foreach (AddressableAssetEntry entry in entries)
            {
                if (entry == null)
                    continue;

                bool isKeptEntry = !string.IsNullOrEmpty(keepGuid) && entry.guid == keepGuid;
                if (isKeptEntry)
                    continue;

                bool isManagedAddress = entry.address == GameCfgAddress;
                bool isObsoleteGuid = !string.IsNullOrEmpty(obsoleteGuid) && entry.guid == obsoleteGuid;

                if (isManagedAddress || isObsoleteGuid)
                    settings.RemoveAssetEntry(entry.guid);
            }
        }

        private void SetInitState(string message, Color color)
        {
            if (tip != null)
            {
                tip.text = message;
                tip.style.color = color;
            }

            if (initRoot != null)
                initRoot.style.borderLeftColor = color;
        }

        private static string GetAssetGuid(Object asset)
        {
            if (asset == null)
                return null;

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                return null;

            return AssetDatabase.AssetPathToGUID(path);
        }

        private void AddSymbol()
        {
            if (symbolInputText == null)
                return;

            string symbol = symbolInputText.value?.Trim();
            if (string.IsNullOrEmpty(symbol))
                return;

            if (!IsValidDefineSymbol(symbol))
            {
                Debug.LogWarning($"无效宏名：{symbol}。宏名只能包含字母、数字、下划线，并且不能以数字开头。");
                return;
            }

            SetDefineSymbol(symbol, true);
            symbolInputText.SetValueWithoutNotify(string.Empty);
            RefreshSymbolsDropdown(symbol);
        }

        private void RemoveSymbol()
        {
            if (symbolsDropdown == null || symbolsDropdown.choices == null || symbolsDropdown.choices.Count == 0)
                return;

            string symbolName = symbolsDropdown.value;
            if (string.IsNullOrEmpty(symbolName) || !symbolsDropdown.choices.Contains(symbolName))
                return;

            SetDefineSymbol(symbolName, false);
            RefreshSymbolsDropdown();
        }

        private void RefreshSymbolsDropdown(string preferredSymbol = null)
        {
            if (symbolsDropdown == null)
                return;

            string oldValue = !string.IsNullOrEmpty(preferredSymbol) ? preferredSymbol : symbolsDropdown.value;
            List<string> symbols = GetDefineSymbols();
            symbols.Sort();

            symbolsDropdown.choices = symbols;

            if (!string.IsNullOrEmpty(oldValue) && symbols.Contains(oldValue))
            {
                symbolsDropdown.SetValueWithoutNotify(oldValue);
            }
            else if (symbols.Count > 0)
            {
                symbolsDropdown.SetValueWithoutNotify(symbols[0]);
            }
            else
            {
                symbolsDropdown.SetValueWithoutNotify(string.Empty);
            }
        }

        private static bool SetDefineSymbol(string symbol, bool enabled)
        {
            if (string.IsNullOrEmpty(symbol))
                return false;

            List<string> symbols = GetDefineSymbols();
            bool changed = false;

            if (enabled)
            {
                if (!symbols.Contains(symbol))
                {
                    symbols.Add(symbol);
                    changed = true;
                }
            }
            else
            {
                changed = symbols.RemoveAll(s => s == symbol) > 0;
            }

            if (!changed)
                return false;

            PlayerSettings.SetScriptingDefineSymbols(GetCurrentNamedBuildTarget(), string.Join(";", symbols));
            return true;
        }

        private static List<string> GetDefineSymbols()
        {
            string symbols = PlayerSettings.GetScriptingDefineSymbols(GetCurrentNamedBuildTarget());
            string[] split = symbols.Split(';');
            List<string> result = new List<string>();

            foreach (string item in split)
            {
                string symbol = item.Trim();
                if (string.IsNullOrEmpty(symbol))
                    continue;

                if (!result.Contains(symbol))
                    result.Add(symbol);
            }

            return result;
        }

        private static UnityEditor.Build.NamedBuildTarget GetCurrentNamedBuildTarget()
        {
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            return UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
        }

        private static bool IsValidDefineSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return false;

            if (!IsValidDefineStartChar(symbol[0]))
                return false;

            for (int i = 1; i < symbol.Length; i++)
            {
                if (!IsValidDefineChar(symbol[i]))
                    return false;
            }

            return true;
        }

        private static bool IsValidDefineStartChar(char c)
        {
            return c == '_' || char.IsLetter(c);
        }

        private static bool IsValidDefineChar(char c)
        {
            return c == '_' || char.IsLetterOrDigit(c);
        }
    }
}
#endif
