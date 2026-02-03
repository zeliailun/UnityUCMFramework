using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public interface IEntityMgr : IDearMgr
    {
        int entityCount { get; }
        int entityGroupDCount { get; }
        IEntity CreateEntity(string entityName, string groupName, Type className, string config = null, Action<IEntity, GameObject> entCreated = null);
        T CreateEntity<T>(string entityName, string groupName, string config = null,Action<IEntity, GameObject> entCreated = null) where T : IEntity, new();
        void ReleaseEntity(EntityId id);
        void ReleaseEntity<T>(T ent) where T : IEntity;
        void ReleaseAllEntity();
        void ShowEntity(EntityId id);
        void ShowAllEntity();
        void HideEntity(EntityId id);
        void HideAllEntity();
        IEntity GetEntity(EntityId id);
        bool IsValidEntity(EntityId id);
        bool IsValidEntity<T>(T ent) where T : IEntity;
        void SetGroup(string groupName, IEntity entity);
        bool HasGroup(string groupName);
        void ShowGroup(string groupName);
        void HideGroup(string groupName);
        void RemoveGroup(string groupName);
        void ClearGroup(string groupName);
        IEntityGroup GetGroup(string groupName);
        void RemoveEntityGroup<T>(T ent) where T : IEntity;
        IEntityGroup GetEntityGroup<T>(T ent) where T : IEntity;
        List<IEntityGroup> GetAllEntityGroup(bool isCopy);
        List<IEntity> GetAllEntity(bool isCopy);
    }
}