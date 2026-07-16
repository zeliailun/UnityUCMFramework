using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [DisallowMultipleComponent]
    public sealed class UnitVfxTarget : MonoBase
    {
        [Header("身体模型")]
        [SerializeField]
        private Renderer[] bodyRenderers = Array.Empty<Renderer>();

        [Header("身体特效附着点")]
        [SerializeField]
        private Transform[] bodyAnchors = Array.Empty<Transform>();

        [Header("身体缩放参考")]
        [SerializeField]
        private Transform scaleReference;

        private Vector3 defaultLossyScale;


        public Renderer[] BodyRenderers => bodyRenderers;

        public Transform[] BodyAnchors => bodyAnchors;

        public int RendererCount => bodyRenderers?.Length ?? 0;

        public int AnchorCount => bodyAnchors?.Length ?? 0;

        public bool HasRenderer => RendererCount > 0;

        public bool HasAnchor => AnchorCount > 0;


        public override void Awake()
        {
            if (scaleReference == null)
                scaleReference = transform;

            defaultLossyScale =
                GetAbsScale(scaleReference.lossyScale);
        }


        public Renderer GetRenderer(int index)
        {
            if (bodyRenderers == null ||
                index < 0 ||
                index >= bodyRenderers.Length)
            {
                return null;
            }

            return bodyRenderers[index];
        }


        public Transform GetAnchor(int index)
        {
            if (bodyAnchors == null ||
                index < 0 ||
                index >= bodyAnchors.Length)
            {
                return null;
            }

            return bodyAnchors[index];
        }


        /// <summary>
        /// 获取身体相对于初始状态的动态缩放倍率。
        /// 例如初始为 1，当前放大到 2，则返回 2。
        /// </summary>
        public float GetScaleRatio()
        {
            if (scaleReference == null)
                return 1f;

            Vector3 currentScale =
                GetAbsScale(scaleReference.lossyScale);

            float x = GetAxisRatio(
                currentScale.x,
                defaultLossyScale.x);

            float y = GetAxisRatio(
                currentScale.y,
                defaultLossyScale.y);

            float z = GetAxisRatio(
                currentScale.z,
                defaultLossyScale.z);

            // 使用平均倍率。
            // 正常怪物统一缩放时 X/Y/Z 本来就是相同值。
            return Mathf.Max(
                (x + y + z) / 3f,
                0.01f);
        }


        private static float GetAxisRatio(
            float current,
            float original)
        {
            if (original <= Mathf.Epsilon)
                return 1f;

            return current / original;
        }


        private static Vector3 GetAbsScale(
            Vector3 scale)
        {
            return new Vector3(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z));
        }


#if UNITY_EDITOR

        [ContextMenu("自动收集身体 Renderer")]
        private void CollectBodyRenderers()
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(true);

            var result =
                new List<Renderer>(renderers.Length);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer is SkinnedMeshRenderer ||
                    renderer is MeshRenderer)
                {
                    result.Add(renderer);
                }
            }

            bodyRenderers = result.ToArray();
        }


        [ContextMenu("清空身体 Renderer")]
        private void ClearBodyRenderers()
        {
            bodyRenderers = Array.Empty<Renderer>();
        }


        [ContextMenu("清空身体 Anchor")]
        private void ClearBodyAnchors()
        {
            bodyAnchors = Array.Empty<Transform>();
        }

#endif
    }
}