using UnityEngine;

namespace UnknownCreator.Modules
{
    /// <summary>
    /// 框架级材质覆盖管理接口。
    /// 外部玩法代码建议依赖这个接口，而不是直接依赖具体管理器实现。
    /// </summary>
    public interface IMaterialOverrideMgr:IDearMgr
    {
        /// <summary>
        /// 注册一个对象，并缓存其子级 Renderer 的原始材质。
        /// </summary>
        void RegisterObject(EntityId id, Transform parentT);

        /// <summary>
        /// 注销对象，恢复原始材质，并回收当前压入的材质实例。
        /// </summary>
        void UnregisterObject(EntityId id);

        /// <summary>
        /// 重新扫描对象当前子级 Renderer。
        /// 常用于模型替换、换皮、重新加载子物体之后。
        /// </summary>
        void RefreshObject(EntityId id, bool useCurrentChildren = true);

        /// <summary>
        /// 兼容旧命名。
        /// 如果旧代码已经在调用 SwapModel，可以先不急着全部改。
        /// </summary>
        void SwapModel(EntityId id, bool useCurrentChildren = true);

        /// <summary>
        /// 压入一个材质覆盖。
        /// priority 越高，显示优先级越高。
        /// target 为空时，作用于所有已注册对象。
        /// </summary>
        void PushMaterial(string materialName, int priority, MaterialTarget? target = null);

        /// <summary>
        /// 弹出一个指定材质覆盖。
        /// target 为空时，作用于所有已注册对象。
        /// </summary>
        void PopMaterial(string materialName, MaterialTarget? target = null);

        /// <summary>
        /// 锁定指定 Renderer。锁定后不会被 Push/Pop/Apply 修改。
        /// </summary>
        void LockRenderer(EntityId entityId, string rendererName);

        /// <summary>
        /// 解锁指定 Renderer。
        /// </summary>
        void UnlockRenderer(EntityId entityId, string rendererName);

        /// <summary>
        /// 查询指定 Renderer 是否被锁定。
        /// </summary>
        bool IsRendererLocked(EntityId entityId, string rendererName);

        /// <summary>
        /// 立即应用所有脏标记 Renderer 的材质结果。
        /// 一般外部不需要手动调用，Push/Pop 和 UpdateMGR 内部会调用。
        /// </summary>
        void ApplyMaterials();
    }
}
