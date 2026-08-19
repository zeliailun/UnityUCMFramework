#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class MergeToSkinnedMeshWindow : EditorWindow
{
    private GameObject source;
    private Material sharedMaterial;
    private bool hideOriginals = true;
    private string savePrefabPath = string.Empty;

    [MenuItem(MergeToSkinnedMesh.ToolsMenuPath, false, 2000)]
    private static void OpenWindow()
    {
        MergeToSkinnedMeshWindow window = GetWindow<MergeToSkinnedMeshWindow>();
        window.titleContent = new GUIContent("Merge To Skinned Mesh");
        window.minSize = new Vector2(460f, 210f);
        window.SetSource(Selection.activeGameObject);
        window.Show();
        window.Focus();
    }

    [MenuItem(MergeToSkinnedMesh.ToolsMenuPath, true)]
    private static bool ValidateOpenWindow()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Mesh To Skinned Mesh", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        EditorGUI.BeginChangeCheck();
        GameObject newSource = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Source Model"),
            source,
            typeof(GameObject),
            true);
        if (EditorGUI.EndChangeCheck())
            SetSource(newSource);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Use Selection", GUILayout.Width(110f)))
                SetSource(Selection.activeGameObject);
        }

        sharedMaterial = (Material)EditorGUILayout.ObjectField(
            new GUIContent("Shared Material"),
            sharedMaterial,
            typeof(Material),
            false);
        hideOriginals = EditorGUILayout.Toggle(
            new GUIContent("Hide Originals"),
            hideOriginals);

        using (new EditorGUILayout.HorizontalScope())
        {
            savePrefabPath = EditorGUILayout.TextField(
                new GUIContent("Save Prefab Path"),
                savePrefabPath);

            if (GUILayout.Button("Browse", GUILayout.Width(72f)))
                BrowseSavePath();
        }

        bool hasMesh = MergeToSkinnedMesh.HasConvertibleMesh(source);
        bool hasValidPath = MergeToSkinnedMesh.IsValidPrefabPath(savePrefabPath);

        if (source != null && !hasMesh)
        {
            EditorGUILayout.HelpBox(
                "Source Model must contain at least one MeshFilter and MeshRenderer.",
                MessageType.Warning);
        }
        else if (!string.IsNullOrWhiteSpace(savePrefabPath) && !hasValidPath)
        {
            EditorGUILayout.HelpBox(
                "Save Prefab Path must point to an existing folder under Assets and end with .prefab.",
                MessageType.Warning);
        }

        GUILayout.FlexibleSpace();
        using (new EditorGUI.DisabledScope(!hasMesh || !hasValidPath))
        {
            if (GUILayout.Button("Convert And Save", GUILayout.Height(30f)))
            {
                MergeToSkinnedMesh.Convert(
                    source,
                    sharedMaterial,
                    hideOriginals,
                    savePrefabPath);
            }
        }

        EditorGUILayout.Space(8f);
    }

    private void SetSource(GameObject newSource)
    {
        source = newSource;
        if (source == null)
        {
            sharedMaterial = null;
            savePrefabPath = string.Empty;
            Repaint();
            return;
        }

        MergeToSkinnedMesh existingSettings = source.GetComponent<MergeToSkinnedMesh>();
        if (existingSettings != null)
        {
            sharedMaterial = existingSettings.sharedMaterial;
            hideOriginals = existingSettings.hideOriginals;
        }
        else
        {
            MeshRenderer renderer = source.GetComponentInChildren<MeshRenderer>(true);
            sharedMaterial = renderer != null ? renderer.sharedMaterial : null;
            hideOriginals = true;
        }

        savePrefabPath = GetDefaultPrefabPath(source, existingSettings?.savePrefabPath);
        Repaint();
    }

    private void BrowseSavePath()
    {
        string defaultDirectory = MergeToSkinnedMesh.GetDefaultSaveDirectory(source, savePrefabPath);
        string defaultName = source != null
            ? MergeToSkinnedMesh.SanitizeAssetFileName(source.name + "_Skinned")
            : "SkinnedMesh";
        if (!string.IsNullOrWhiteSpace(savePrefabPath) &&
            savePrefabPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
        {
            defaultName = MergeToSkinnedMesh.GetSafeFileNameWithoutExtension(
                savePrefabPath,
                defaultName);
        }

        string selectedPath = EditorUtility.SaveFilePanelInProject(
            "Save Skinned Mesh Prefab",
            defaultName,
            "prefab",
            "Choose where to save the converted Prefab.",
            defaultDirectory);
        if (!string.IsNullOrWhiteSpace(selectedPath))
            savePrefabPath = MergeToSkinnedMesh.NormalizeAssetPath(selectedPath);
    }

    private static string GetDefaultPrefabPath(GameObject model, string configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) &&
            MergeToSkinnedMesh.IsValidPrefabPath(configuredPath))
            return MergeToSkinnedMesh.NormalizeAssetPath(configuredPath);

        string directory = MergeToSkinnedMesh.GetDefaultSaveDirectory(model, string.Empty);
        string safeModelName = MergeToSkinnedMesh.SanitizeAssetFileName(model.name);
        string candidate = $"{directory}/{safeModelName}_Skinned.prefab";
        return AssetDatabase.GenerateUniqueAssetPath(candidate);
    }
}
#endif
