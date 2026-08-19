using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public interface IEntityMgr : IDearMgr
    {
        IReadOnlyList<IEntity> allEnt { get; }
        IReadOnlyList<IEntityGroup> allEntGroup { get; }

        Action <IEntity> OnEntityRegistered { set; get; }
        Action<IEntity> OnEntityReleasing { set; get; }
        int entityCount { get; }
        int entityGroupDCount { get; }
        void RegisterEntity(IEntity entity, string groupName);
        void ReleaseEntity(EntityId id);
        void ReleaseEntity<T>(T ent) where T : IEntity;
        void ReleaseAllEntity();
        void ShowEntity(EntityId id);
        void ShowAllEntity();
        void HideEntity(EntityId id);
        void HideAllEntity();
        IEntity GetEntity(EntityId id);
        bool IsVaildEntity(EntityId id);
        bool IsVaildEntity<T>(T ent) where T : IEntity;
        void SetGroup(string groupName, IEntity entity);
        bool HasGroup(string groupName);
        void ShowGroup(string groupName);
        void HideGroup(string groupName);
        void RemoveGroup(string groupName);
        void ClearGroup(string groupName);
        IEntityGroup GetGroup(string groupName);
        void RemoveEntityGroup<T>(T ent) where T : IEntity;
        IEntityGroup GetEntityGroup<T>(T ent) where T : IEntity;
    }
}
