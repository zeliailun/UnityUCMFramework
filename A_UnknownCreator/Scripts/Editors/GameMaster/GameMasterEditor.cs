#if UNITY_EDITOR
using Unity;
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
        private const string gameCfgName = "UCMGameCfg";

        private VisualTreeAsset m_VisualTreeAsset;
        private VisualElement root => rootVisualElement;
        private VisualElement initRoot;
        private Label tip;
        private TextField symbolInputText;
        private DropdownField symbolsDropdown;
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
            if (m_VisualTreeAsset == null)
                m_VisualTreeAsset = EditorUtils.GetAsset<VisualTreeAsset>(visualTreeName);

            if (gameCfgSO == null)
                gameCfgSO = EditorUtils.GetAsset<GameCfgSO>(gameCfgName);


            root.Add(m_VisualTreeAsset.CloneTree());
            root.Q<Button>("Build").clicked += Init;
            initRoot = root.Q<VisualElement>("InitRoot");
            tip = root.Q<Label>("Tip");

            AddressableAssetGroup defGroup = AddressableAssetSettingsDefaultObject.Settings.FindGroup(groupName);
            if (defGroup == null)
            {
                tip.text = "未初始化模块";
                tip.style.color = Color.red;
                initRoot.style.borderLeftColor = Color.red;
            }
            else
            {
                tip.text = "已初始化模块";
                tip.style.color = Color.green;
                initRoot.style.borderLeftColor = Color.green;
            }


            //===========================================================================



            symbolInputText = root.Q<TextField>("SymbolInputText");
            symbolsDropdown = root.Q<DropdownField>("SymbolsList");
            root.Q<Button>("AddSymbol").clicked += AddSymbol;
            root.Q<Button>("RemoveSymbol").clicked += RemoveSymbol;
            InitSymbol();

            //===========================================================================


            var inspector = new InspectorElement(gameCfgSO);
            if (inspector != null)
                root.Q<VisualElement>("Mgr").Add(inspector);
        }

        private void OnInspectorUpdate()
        {
            if (gameCfgSO == null)
            {
                tip.text = "未初始化模块";
                tip.style.color = Color.red;
                initRoot.style.borderLeftColor = Color.red;
            }
        }

        private void Init()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
                group = settings.CreateGroup(groupName, false, true, false, settings.DefaultGroup.Schemas);

            if (gameCfgSO == null)
            {
                UCMDebug.LogWarning("不能设置空的配置文件到Addressable");
                return;
            }
            var path = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(gameCfgSO));
            if (group.GetAssetEntry(path) == null)
            {
                var entry = settings.CreateOrMoveEntry(path, group);
                entry.address = gameCfgSO.name;
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }
            tip.text = "已初始化模块";
            tip.style.color = Color.green;
            initRoot.style.borderLeftColor = Color.green;
            AssetDatabase.SaveAssets();
        }

        private void InitSymbol()
        {


            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(target);

            if (!symbols.Contains("UCMDebug"))
            {
                var newSymbols = symbols + ";" + "UCMDebug";
                PlayerSettings.SetScriptingDefineSymbols(target, newSymbols);
            }

            string[] ss = symbols.Split(';');

            for (int i = 0; i < ss.Length; i++)
            {
                if (!symbolsDropdown.choices.Contains(ss[i]))
                    symbolsDropdown.choices.Add(ss[i]);
            }
        }
        private void AddSymbol()
        {
            if (string.IsNullOrWhiteSpace(symbolInputText.value) ||
                symbolsDropdown.choices.Contains(symbolInputText.value)) return;

            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(target);
            if (!symbols.Contains(symbolInputText.value))
            {
                var newSymbols = symbols + ";" + symbolInputText.value;
                PlayerSettings.SetScriptingDefineSymbols(target, newSymbols);
                symbolsDropdown.choices.Add(symbolInputText.value);
                UCMDebug.Log("已添加宏");
            }
        }
        private void RemoveSymbol()
        {
            if (symbolsDropdown.index < 0 ||
                symbolsDropdown.index > symbolsDropdown.choices.Count) return;
            var symbolName = symbolsDropdown.choices[symbolsDropdown.index];
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(target);
            string[] ss = symbols.Split(';');
            for (int i = 0; i < ss.Length; i++)
            {
                if (ss[i] == symbolsDropdown.choices[symbolsDropdown.index])
                {
                    ss[i] = null;
                    PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", ss));
                    symbolsDropdown.choices.Remove(symbolName);
                    UCMDebug.Log("已删除宏");
                    return;
                }
            }


        }

    }
}
#endif
