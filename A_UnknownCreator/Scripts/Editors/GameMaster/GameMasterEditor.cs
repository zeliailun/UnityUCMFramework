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
        private const string groupName = "UCM";
        private const string visualTreeName = "GameMasterEditor";
        private const string cfgKey = "GM_SelectedGameCfg";

        private VisualTreeAsset m_VisualTreeAsset;
        private VisualElement root => rootVisualElement;
        private VisualElement initRoot;
        private Label tip;
        private TextField symbolInputText;
        private DropdownField symbolsDropdown;
        private ObjectField cfgSelector;

        private GameCfgSO gameCfgSO;

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
            m_VisualTreeAsset = UnityEditorGlobals.GetAsset<VisualTreeAsset>(visualTreeName);
            root.Add(m_VisualTreeAsset.CloneTree());

            initRoot = root.Q<VisualElement>("InitRoot");
            tip = root.Q<Label>("Tip");

            symbolInputText = root.Q<TextField>("SymbolInputText");
            symbolsDropdown = root.Q<DropdownField>("SymbolsList");
            root.Q<Button>("AddSymbol").clicked += AddSymbol;
            root.Q<Button>("RemoveSymbol").clicked += RemoveSymbol;

            cfgSelector = root.Q<ObjectField>("GameCfg");
            cfgSelector.objectType = typeof(GameCfgSO);
            cfgSelector.allowSceneObjects = false;
            cfgSelector.RegisterValueChangedCallback(evt =>
            {
                gameCfgSO = evt.newValue as GameCfgSO;

                var mgr = root.Q<VisualElement>("Mgr");
                mgr.Clear();
                if (gameCfgSO != null)
                    mgr.Add(new InspectorElement(gameCfgSO));

                SaveCfg();

                Init();
            });

            LoadCfg();
            Init();
        }

        private void SaveCfg()
        {
            if (gameCfgSO != null)
            {
                string guid = AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(gameCfgSO));
                EditorPrefs.SetString(cfgKey, guid);
            }
            else
            {
                EditorPrefs.DeleteKey(cfgKey);
            }
        }

        private void LoadCfg()
        {
            if (EditorPrefs.HasKey(cfgKey))
            {
                string guid = EditorPrefs.GetString(cfgKey);
                string path = AssetDatabase.GUIDToAssetPath(guid);
                gameCfgSO = AssetDatabase.LoadAssetAtPath<GameCfgSO>(path);

                if (cfgSelector != null)
                    cfgSelector.value = gameCfgSO;

                var mgr = root.Q<VisualElement>("Mgr");
                mgr.Clear();
                if (gameCfgSO != null)
                    mgr.Add(new InspectorElement(gameCfgSO));
            }
        }

        private void Init()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            AddressableAssetGroup group = settings.FindGroup(groupName);

            // =========================================================
            // 如果 gameCfgSO 为空 → 自动回滚
            // =========================================================
            if (gameCfgSO == null)
            {
                if (group != null)
                {
                    var entries = group.entries.ToList();
                    foreach (var entry in entries)
                    {
                        settings.RemoveAssetEntry(entry.guid);
                    }
                }

                // 移除 UCMDebug 宏
                var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
                var symbols = PlayerSettings.GetScriptingDefineSymbols(target);

                if (symbols.Contains("UCMDebug"))
                {
                    var list = symbols.Split(';')
                                      .Where(s => !string.IsNullOrEmpty(s))
                                      .ToList();

                    list.RemoveAll(s => s == "UCMDebug");
                    PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list));
                }

                tip.text = "未初始化模块";
                tip.style.color = Color.red;
                initRoot.style.borderLeftColor = Color.red;

                AssetDatabase.SaveAssets();
                return;
            }

            // =========================================================
            // 正常初始化
            // =========================================================

            if (group == null)
                group = settings.CreateGroup(groupName, false, true, false, settings.DefaultGroup.Schemas);

            var guid = AssetDatabase.AssetPathToGUID(
                AssetDatabase.GetAssetPath(gameCfgSO));

            if (group.GetAssetEntry(guid) == null)
            {
                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = gameCfgSO.name;
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }

            tip.text = "已初始化模块";
            tip.style.color = Color.green;
            initRoot.style.borderLeftColor = Color.green;

            AssetDatabase.SaveAssets();

            // 添加 UCMDebug 宏
            var btg = EditorUserBuildSettings.selectedBuildTargetGroup;
            var t = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(btg);
            var currentSymbols = PlayerSettings.GetScriptingDefineSymbols(t);

            if (!currentSymbols.Contains("UCMDebug"))
            {
                var list = currentSymbols.Split(';')
                                         .Where(s => !string.IsNullOrEmpty(s))
                                         .ToList();

                list.Add("UCMDebug");
                PlayerSettings.SetScriptingDefineSymbols(t, string.Join(";", list));
            }
        }

        private void AddSymbol()
        {
            if (string.IsNullOrWhiteSpace(symbolInputText.value))
                return;

            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(target);

            var list = symbols.Split(';')
                              .Where(s => !string.IsNullOrEmpty(s))
                              .ToList();

            if (!list.Contains(symbolInputText.value))
            {
                list.Add(symbolInputText.value);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list));
                symbolsDropdown.choices.Add(symbolInputText.value);
            }
        }

        private void RemoveSymbol()
        {
            if (symbolsDropdown.index < 0 ||
                symbolsDropdown.index >= symbolsDropdown.choices.Count)
                return;

            var symbolName = symbolsDropdown.choices[symbolsDropdown.index];

            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(target);

            var list = symbols.Split(';')
                              .Where(s => !string.IsNullOrEmpty(s))
                              .ToList();

            if (list.Contains(symbolName))
            {
                list.RemoveAll(s => s == symbolName);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list));
                symbolsDropdown.choices.Remove(symbolName);
            }
        }
    }
}
#endif
