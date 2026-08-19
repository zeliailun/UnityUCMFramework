
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class MergeToSkinnedMesh : MonoBehaviour
{
    public const string ToolsMenuPath = "UnknownCreator/Merge To Skinned Mesh";

    [Tooltip("统一材质")]
    public Material sharedMaterial;

    [Tooltip("是否隐藏原方块")]
    public bool hideOriginals = true;

    [Tooltip("Prefab 保存路径，例如 Assets/Voxel.prefab")]
    public string savePrefabPath = "Assets/Voxel.prefab";

    private static void MergeSelectedFromToolsMenu()
    {
        GameObject source = Selection.activeGameObject;
        if (!HasConvertibleMesh(source))
        {
            EditorUtility.DisplayDialog(
                "Merge To Skinned Mesh",
                "请先在 Hierarchy 或 Project 面板中选择一个包含 MeshFilter 和 MeshRenderer 的模型。",
                "确定");
            return;
        }

        MergeToSkinnedMesh sourceMerger = source.GetComponent<MergeToSkinnedMesh>();
        string currentPath = sourceMerger != null ? sourceMerger.savePrefabPath : string.Empty;
        string defaultDirectory = GetDefaultSaveDirectory(source, currentPath);
        string defaultName = SanitizeAssetFileName(source.name);

        if (!string.IsNullOrWhiteSpace(currentPath) &&
            currentPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
        {
            defaultName = GetSafeFileNameWithoutExtension(currentPath, defaultName);
        }

        string prefabPath = EditorUtility.SaveFilePanelInProject(
            "Save Skinned Mesh Prefab",
            defaultName,
            "prefab",
            "Choose where to save the converted prefab.",
            defaultDirectory);
        if (string.IsNullOrWhiteSpace(prefabPath))
            return;

        bool destroyConversionRoot = EditorUtility.IsPersistent(source);
        GameObject conversionRoot = destroyConversionRoot
            ? CreateTemporaryInstance(source)
            : source;
        if (conversionRoot == null)
        {
            Debug.LogError($"Failed to instantiate selected model: {source.name}", source);
            return;
        }

        MergeToSkinnedMesh merger = conversionRoot.GetComponent<MergeToSkinnedMesh>();
        bool addedForMenu = merger == null;
        if (addedForMenu)
            merger = conversionRoot.AddComponent<MergeToSkinnedMesh>();

        merger.savePrefabPath = NormalizeAssetPath(prefabPath);

        if (merger.sharedMaterial == null)
        {
            MeshRenderer meshRenderer = conversionRoot.GetComponentInChildren<MeshRenderer>(true);
            if (meshRenderer != null)
                merger.sharedMaterial = meshRenderer.sharedMaterial;
        }

        try
        {
            merger.MergeAndSavePrefab();
        }
        finally
        {
            if (addedForMenu && merger != null)
                DestroyImmediate(merger);

            if (destroyConversionRoot && conversionRoot != null)
                DestroyImmediate(conversionRoot);
        }
    }

    private static bool ValidateMergeSelectedFromToolsMenu()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    public static void Convert(
        GameObject source,
        Material material,
        bool shouldHideOriginals,
        string prefabPath)
    {
        if (!HasConvertibleMesh(source))
        {
            Debug.LogError("The source model must contain a MeshFilter and MeshRenderer.", source);
            return;
        }

        if (!IsValidPrefabPath(prefabPath))
        {
            Debug.LogError($"Invalid Prefab path: {prefabPath}", source);
            return;
        }

        bool destroyConversionRoot = EditorUtility.IsPersistent(source);
        GameObject conversionRoot = destroyConversionRoot
            ? CreateTemporaryInstance(source)
            : source;
        if (conversionRoot == null)
        {
            Debug.LogError($"Failed to instantiate selected model: {source.name}", source);
            return;
        }

        MergeToSkinnedMesh merger = conversionRoot.GetComponent<MergeToSkinnedMesh>();
        bool addedForConversion = merger == null;
        if (addedForConversion)
            merger = conversionRoot.AddComponent<MergeToSkinnedMesh>();

        merger.sharedMaterial = material;
        merger.hideOriginals = shouldHideOriginals;
        merger.savePrefabPath = NormalizeAssetPath(prefabPath);

        try
        {
            merger.MergeAndSavePrefab();
        }
        finally
        {
            if (addedForConversion && merger != null)
                DestroyImmediate(merger);

            if (destroyConversionRoot && conversionRoot != null)
                DestroyImmediate(conversionRoot);
        }
    }

    public static bool HasConvertibleMesh(GameObject root)
    {
        if (root == null)
            return false;

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh != null && meshFilter.GetComponent<MeshRenderer>() != null)
                return true;
        }

        return false;
    }

    public static string GetDefaultSaveDirectory(GameObject source, string currentPath)
    {
        if (TryGetAssetDirectory(currentPath, out string currentDirectory))
            return currentDirectory;

        string sourcePath = AssetDatabase.GetAssetPath(source);
        return TryGetAssetDirectory(sourcePath, out string sourceDirectory)
            ? sourceDirectory
            : "Assets";
    }

    public static string NormalizeAssetPath(string assetPath)
    {
        return string.IsNullOrWhiteSpace(assetPath)
            ? string.Empty
            : assetPath.Trim().Replace('\\', '/');
    }

    public static string GetSafeFileNameWithoutExtension(string assetPath, string fallbackName)
    {
        string normalizedPath = NormalizeAssetPath(assetPath);
        int slashIndex = normalizedPath.LastIndexOf('/');
        string fileName = slashIndex >= 0
            ? normalizedPath.Substring(slashIndex + 1)
            : normalizedPath;

        const string prefabExtension = ".prefab";
        if (fileName.EndsWith(prefabExtension, System.StringComparison.OrdinalIgnoreCase))
            fileName = fileName.Substring(0, fileName.Length - prefabExtension.Length);

        return SanitizeAssetFileName(fileName, fallbackName);
    }

    public static string SanitizeAssetFileName(string fileName, string fallbackName = "SkinnedMesh")
    {
        string safeFallback = SanitizeFileNameCharacters(fallbackName);
        if (string.IsNullOrWhiteSpace(safeFallback))
            safeFallback = "SkinnedMesh";

        if (string.IsNullOrWhiteSpace(fileName))
            return safeFallback;

        string sanitizedName = SanitizeFileNameCharacters(fileName);
        return string.IsNullOrWhiteSpace(sanitizedName)
            ? safeFallback
            : sanitizedName;
    }

    private static string SanitizeFileNameCharacters(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        char[] characters = fileName.Trim().ToCharArray();
        for (int i = 0; i < characters.Length; i++)
        {
            char character = characters[i];
            if (char.IsControl(character) ||
                character == '/' || character == '\\' || character == ':' ||
                character == '*' || character == '?' || character == '"' ||
                character == '<' || character == '>' || character == '|')
            {
                characters[i] = '_';
            }
        }

        return new string(characters).Trim(' ', '.');
    }

    private static bool TryGetAssetDirectory(string assetPath, out string directory)
    {
        directory = string.Empty;
        string normalizedPath = NormalizeAssetPath(assetPath);
        if (!normalizedPath.StartsWith("Assets/", System.StringComparison.Ordinal) ||
            ContainsInvalidAssetPathCharacter(normalizedPath))
            return false;

        int slashIndex = normalizedPath.LastIndexOf('/');
        if (slashIndex < "Assets".Length)
            return false;

        string candidate = normalizedPath.Substring(0, slashIndex);
        if (!AssetDatabase.IsValidFolder(candidate))
            return false;

        directory = candidate;
        return true;
    }

    private static bool ContainsInvalidAssetPathCharacter(string assetPath)
    {
        for (int i = 0; i < assetPath.Length; i++)
        {
            char character = assetPath[i];
            if (char.IsControl(character) || character == ':' || character == '"' ||
                character == '<' || character == '>' || character == '|' ||
                character == '?' || character == '*')
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject CreateTemporaryInstance(GameObject source)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null)
            return null;

        if (PrefabUtility.IsPartOfPrefabInstance(instance))
        {
            PrefabUtility.UnpackPrefabInstance(
                instance,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
        }

        instance.name = source.name;
        return instance;
    }

    [ContextMenu("Merge Voxels and Save Prefab")]
    public void MergeAndSavePrefab()
    {
        GameObject root = gameObject;
        string normalizedPrefabPath = NormalizeAssetPath(savePrefabPath);
        if (!IsValidPrefabPath(normalizedPrefabPath))
        {
            Debug.LogError($"Invalid Prefab path: {savePrefabPath}", root);
            return;
        }

        savePrefabPath = normalizedPrefabPath;

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
        if (meshFilters.Length == 0)
        {
            Debug.LogWarning("No MeshFilters found!");
            return;
        }

        List<VoxelPiece> voxelPieces = new List<VoxelPiece>();
        foreach (var mf in meshFilters)
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            Mesh mesh = mf.sharedMesh;
            if (mr == null || mesh == null || mesh.vertexCount == 0)
                continue;

            if (!mesh.isReadable)
            {
                Debug.LogError(
                    $"Mesh '{mesh.name}' is not readable. Enable Read/Write in its import settings before converting.",
                    mf);
                return;
            }

            Transform bone = FindNearestBone(mf.transform);
            voxelPieces.Add(new VoxelPiece
            {
                meshFilter = mf,
                meshRenderer = mr,
                bone = bone
            });
        }

        if (voxelPieces.Count == 0)
        {
            Debug.LogWarning("No voxel pieces detected!");
            return;
        }

        // 生成骨骼列表
        List<Transform> bones = new List<Transform>();
        Dictionary<Transform, int> boneIndexMap = new Dictionary<Transform, int>();
        int idx = 0;
        foreach (var vp in voxelPieces)
        {
            if (!boneIndexMap.ContainsKey(vp.bone))
            {
                boneIndexMap[vp.bone] = idx++;
                bones.Add(vp.bone);
            }
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<BoneWeight> boneWeights = new List<BoneWeight>();
        List<int> triangles = new List<int>();
        bool recalculateNormals = false;

        int vertexOffset = 0;

        foreach (var vp in voxelPieces)
        {
            Mesh mesh = vp.meshFilter.sharedMesh;
            int boneIndex = boneIndexMap[vp.bone];
            Matrix4x4 localToRoot = transform.worldToLocalMatrix * vp.meshFilter.transform.localToWorldMatrix;

            foreach (var v in mesh.vertices)
                vertices.Add(localToRoot.MultiplyPoint3x4(v));

            Vector3[] meshNormals = mesh.normals;
            if (meshNormals.Length == mesh.vertexCount)
            {
                Matrix4x4 normalMatrix = localToRoot.inverse.transpose;
                foreach (var n in meshNormals)
                    normals.Add(normalMatrix.MultiplyVector(n).normalized);
            }
            else
            {
                recalculateNormals = true;
                for (int i = 0; i < mesh.vertexCount; i++)
                    normals.Add(Vector3.zero);
            }

            Vector2[] meshUvs = mesh.uv;
            if (meshUvs.Length == mesh.vertexCount)
            {
                uvs.AddRange(meshUvs);
            }
            else
            {
                for (int i = 0; i < mesh.vertexCount; i++)
                    uvs.Add(Vector2.zero);
            }

            foreach (var t in mesh.triangles)
                triangles.Add(t + vertexOffset);

            for (int i = 0; i < mesh.vertexCount; i++)
            {
                BoneWeight bw = new BoneWeight();
                bw.boneIndex0 = boneIndex;
                bw.weight0 = 1f;
                boneWeights.Add(bw);
            }

            vertexOffset += mesh.vertexCount;
        }

        // 创建合并 Mesh
        Mesh mergedMesh = new Mesh();
        mergedMesh.name = "VoxelMerged";
        mergedMesh.indexFormat = vertices.Count > ushort.MaxValue
            ? IndexFormat.UInt32
            : IndexFormat.UInt16;
        mergedMesh.vertices = vertices.ToArray();
        mergedMesh.uv = uvs.ToArray();
        mergedMesh.triangles = triangles.ToArray();
        mergedMesh.boneWeights = boneWeights.ToArray();

        if (recalculateNormals)
            mergedMesh.RecalculateNormals();
        else
            mergedMesh.normals = normals.ToArray();

        Matrix4x4[] bindPoses = new Matrix4x4[bones.Count];
        for (int i = 0; i < bones.Count; i++)
        {
            bindPoses[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
        }
        mergedMesh.bindposes = bindPoses;
        mergedMesh.RecalculateBounds();

        // 保存 Mesh Asset
        string meshAssetPath = GetMeshAssetPath(savePrefabPath);
        Mesh savedMesh = SaveMeshAsset(mergedMesh, meshAssetPath);
        if (savedMesh == null)
            return;

        AssetDatabase.SaveAssets();

        // 添加/更新 SkinnedMeshRenderer
        SkinnedMeshRenderer smr = root.GetComponent<SkinnedMeshRenderer>();
        if (smr == null)
            smr = Undo.AddComponent<SkinnedMeshRenderer>(root);
        else
            Undo.RecordObject(smr, "Configure Skinned Mesh Renderer");

        smr.sharedMesh = savedMesh;
        smr.bones = bones.ToArray();
        smr.rootBone = transform;
        smr.sharedMaterial = sharedMaterial != null
            ? sharedMaterial
            : voxelPieces[0].meshRenderer.sharedMaterial;
        smr.localBounds = savedMesh.bounds;
        EditorUtility.SetDirty(smr);

        if (hideOriginals)
        {
            foreach (var vp in voxelPieces)
            {
                Undo.RecordObject(vp.meshRenderer, "Hide Original Mesh Renderer");
                vp.meshRenderer.enabled = false;
                EditorUtility.SetDirty(vp.meshRenderer);
            }
        }

        // 保存为 Prefab
        string prefabPath = savePrefabPath;
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
            root,
            prefabPath,
            InteractionMode.UserAction,
            out bool saveSucceeded);
        if (!saveSucceeded || prefab == null)
        {
            Debug.LogError($"Failed to save converted Prefab: {prefabPath}", root);
            return;
        }

        RemoveConverterFromPrefab(prefabPath);

        Debug.Log($"转换完成: {prefabPath}");
    }

    public static bool IsValidPrefabPath(string prefabPath)
    {
        string normalizedPath = NormalizeAssetPath(prefabPath);
        if (!normalizedPath.StartsWith("Assets/", System.StringComparison.Ordinal) ||
            !normalizedPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase) ||
            !TryGetAssetDirectory(normalizedPath, out _))
            return false;

        int slashIndex = normalizedPath.LastIndexOf('/');
        string fileName = normalizedPath.Substring(slashIndex + 1);
        string sanitizedFileName = SanitizeAssetFileName(fileName, string.Empty);
        return !string.IsNullOrWhiteSpace(sanitizedFileName) &&
               string.Equals(fileName, sanitizedFileName, System.StringComparison.Ordinal);
    }

    private static string GetMeshAssetPath(string prefabPath)
    {
        int extensionIndex = prefabPath.LastIndexOf(".prefab", System.StringComparison.OrdinalIgnoreCase);
        return prefabPath.Substring(0, extensionIndex) + "SkinnedMesh.asset";
    }

    private static Mesh SaveMeshAsset(Mesh mergedMesh, string meshAssetPath)
    {
        Object existingAsset = AssetDatabase.LoadMainAssetAtPath(meshAssetPath);
        if (existingAsset != null && existingAsset is not Mesh)
        {
            Debug.LogError($"A non-Mesh asset already exists at: {meshAssetPath}");
            DestroyImmediate(mergedMesh);
            return null;
        }

        if (existingAsset is Mesh existingMesh)
        {
            EditorUtility.CopySerialized(mergedMesh, existingMesh);
            EditorUtility.SetDirty(existingMesh);
            DestroyImmediate(mergedMesh);
            return existingMesh;
        }

        AssetDatabase.CreateAsset(mergedMesh, meshAssetPath);
        return mergedMesh;
    }

    private static void RemoveConverterFromPrefab(string prefabPath)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            MergeToSkinnedMesh[] converters = prefabRoot.GetComponentsInChildren<MergeToSkinnedMesh>(true);
            foreach (var converter in converters)
                DestroyImmediate(converter);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private Transform FindNearestBone(Transform t)
    {
        Transform current = t.parent;
        while (current != null)
        {
            string n = current.name.ToLower();
            if (n.Contains("hip") || n.Contains("spine") || n.Contains("arm") || n.Contains("leg") || n.Contains("head"))
                return current;
            current = current.parent;
        }
        return transform; // 默认根骨骼
    }

    private class VoxelPiece
    {
        public Transform bone;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
    }
}
#endif
