using UnityEngine;

namespace UnknownCreator.Modules
{
    public enum BodyVfxBindType
    {
        Auto,
        BodySurface,
        BodyAnchor
    }


    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class BodyVfxEmitter : MonoBase
    {
        [SerializeField]
        private BodyVfxBindType bindType =
            BodyVfxBindType.Auto;

        [SerializeField]
        private ParticleSystemMeshShapeType surfaceType =
            ParticleSystemMeshShapeType.Triangle;

        [SerializeField]
        private float normalOffset = 0.02f;

        [Header("动态身体缩放")]
        [SerializeField]
        private bool followTargetScale = true;

        [SerializeField]
        private float scaleMultiplier = 1f;


        public BodyVfxBindType BindType => bindType;

        public ParticleSystemMeshShapeType SurfaceType =>
            surfaceType;

        public float NormalOffset => normalOffset;

        public bool FollowTargetScale =>
            followTargetScale;

        public float ScaleMultiplier =>
            scaleMultiplier;
    }
}