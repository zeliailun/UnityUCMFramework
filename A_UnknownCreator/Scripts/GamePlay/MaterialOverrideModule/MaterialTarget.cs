using UnityEngine;

namespace UnknownCreator.Modules
{
    /// <summary>
    /// 材质覆盖目标。
    /// entityId：目标对象。
    /// rendererName：指定子渲染器名称；为空时表示该对象下全部 Renderer。
    /// </summary>
    public readonly struct MaterialTarget
    {
        public readonly EntityId entityId;
        public readonly string rendererName;

        public bool IsEmpty => entityId == default && string.IsNullOrEmpty(rendererName);

        public MaterialTarget(EntityId entityId, string rendererName = null)
        {
            this.entityId = entityId;
            this.rendererName = rendererName;
        }
    }
}
