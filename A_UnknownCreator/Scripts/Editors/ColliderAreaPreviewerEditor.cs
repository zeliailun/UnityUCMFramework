#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnknownCreator.Modules
{
    [CustomEditor(typeof(ColliderAreaPreviewer))]
    public class ColliderAreaPreviewerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            ColliderAreaPreviewer previewer = (ColliderAreaPreviewer)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("快速创建碰撞体", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Box"))
                {
                    CreateArea(previewer, () => previewer.CreateBoxArea());
                }

                if (GUILayout.Button("Sphere"))
                {
                    CreateArea(previewer, () => previewer.CreateSphereArea());
                }

                if (GUILayout.Button("Capsule"))
                {
                    CreateArea(previewer, () => previewer.CreateCapsuleArea());
                }
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("所有 Collider 设为 Trigger"))
            {
                Collider[] colliders = previewer.GetComponentsInChildren<Collider>(true);

                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider col = colliders[i];

                    if (col == null)
                        continue;

                    Undo.RecordObject(col, "Set Collider Trigger");
                    col.isTrigger = true;
                    EditorUtility.SetDirty(col);
                }
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "这个组件只用于编辑器非播放状态下的预览。\n进入 Play Mode 时不会删除自身，也不会删除 Collider，只是不再绘制颜色。\n打包时会自动从场景中剥离，只保留空物体和 Collider。",
                MessageType.Info
            );

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateArea(ColliderAreaPreviewer previewer, System.Func<Collider> createFunc)
        {
            Undo.IncrementCurrentGroup();

            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Collider Area");

            Collider col = createFunc.Invoke();

            Undo.RegisterCreatedObjectUndo(col.gameObject, "Create Collider Area");

            Selection.activeGameObject = col.gameObject;
            EditorGUIUtility.PingObject(col.gameObject);

            Undo.CollapseUndoOperations(group);

            EditorUtility.SetDirty(previewer);
        }

        [MenuItem("GameObject/UnknownCreator/Collider Area Group", false, 10)]
        private static void CreateColliderAreaGroup(MenuCommand menuCommand)
        {
            GameObject root = new GameObject("ColliderAreaGroup");

            Undo.RegisterCreatedObjectUndo(root, "Create Collider Area Group");

            GameObjectUtility.SetParentAndAlign(root, menuCommand.context as GameObject);

            ColliderAreaPreviewer previewer = Undo.AddComponent<ColliderAreaPreviewer>(root);

            BoxCollider box = previewer.CreateBoxArea();
            Undo.RegisterCreatedObjectUndo(box.gameObject, "Create Box Area");

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }
    }

    /// <summary>
    /// 打包时自动剥离 ColliderAreaPreviewer。
    ///
    /// 进入 Play Mode 不会删除该组件。
    /// 只有 Build 场景处理阶段才会剥离。
    /// </summary>
    public class ColliderAreaPreviewerBuildStripper : IProcessSceneWithReport
    {
        public int callbackOrder => -1000;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (!scene.IsValid())
                return;

            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                StripFromRoot(roots[i]);
            }
        }

        private void StripFromRoot(GameObject root)
        {
            ColliderAreaPreviewer[] previewers = root.GetComponentsInChildren<ColliderAreaPreviewer>(true);

            for (int i = 0; i < previewers.Length; i++)
            {
                ColliderAreaPreviewer previewer = previewers[i];

                if (previewer == null)
                    continue;

                Object.DestroyImmediate(previewer);
            }
        }
    }
}
#endif
