
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[ExecuteInEditMode]
public class MergeToSkinnedMesh : MonoBehaviour
{
    [Tooltip("统一材质")]
    public Material sharedMaterial;

    [Tooltip("是否隐藏原方块")]
    public bool hideOriginals = true;

    [Tooltip("Prefab 保存路径，例如 Assets/Voxel.prefab")]
    public string savePrefabPath = "Assets/Voxel.prefab";

    [ContextMenu("Merge Voxels and Save Prefab")]
    public void MergeAndSavePrefab()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogWarning("No MeshFilters found!");
            return;
        }

        List<VoxelPiece> voxelPieces = new List<VoxelPiece>();
        foreach (var mf in meshFilters)
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null) continue;

            Transform bone = FindNearestBone(mf.transform);
            voxelPieces.Add(new VoxelPiece { meshFilter = mf, bone = bone });
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

        int vertexOffset = 0;

        foreach (var vp in voxelPieces)
        {
            Mesh mesh = vp.meshFilter.sharedMesh;
            int boneIndex = boneIndexMap[vp.bone];

            foreach (var v in mesh.vertices)
            {
                Vector3 worldPos = vp.meshFilter.transform.TransformPoint(v);
                vertices.Add(transform.InverseTransformPoint(worldPos));
            }

            foreach (var n in mesh.normals)
            {
                Vector3 worldNormal = vp.meshFilter.transform.TransformDirection(n);
                normals.Add(transform.InverseTransformDirection(worldNormal));
            }

            uvs.AddRange(mesh.uv);

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
        mergedMesh.vertices = vertices.ToArray();
        mergedMesh.normals = normals.ToArray();
        mergedMesh.uv = uvs.ToArray();
        mergedMesh.triangles = triangles.ToArray();
        mergedMesh.boneWeights = boneWeights.ToArray();

        Matrix4x4[] bindPoses = new Matrix4x4[bones.Count];
        for (int i = 0; i < bones.Count; i++)
        {
            bindPoses[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
        }
        mergedMesh.bindposes = bindPoses;
        mergedMesh.RecalculateBounds();

        // 保存 Mesh Asset
        string meshAssetPath = savePrefabPath.Replace(".prefab", "SkinnedMesh.asset");
        AssetDatabase.DeleteAsset(meshAssetPath);
        AssetDatabase.CreateAsset(mergedMesh, meshAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 添加/更新 SkinnedMeshRenderer
        SkinnedMeshRenderer smr = gameObject.GetComponent<SkinnedMeshRenderer>();
        if (smr == null) smr = gameObject.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = mergedMesh;
        smr.bones = bones.ToArray();
        smr.rootBone = transform;
        smr.sharedMaterial = sharedMaterial;
        smr.localBounds = mergedMesh.bounds;

        if (hideOriginals)
        {
            foreach (var vp in voxelPieces)
                vp.meshFilter.gameObject.SetActive(false);
        }

        // 保存为 Prefab
        string prefabPath = savePrefabPath;
        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, prefabPath, InteractionMode.UserAction);

        Debug.Log($"转换完成: {prefabPath}");
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
    }
}
#endif