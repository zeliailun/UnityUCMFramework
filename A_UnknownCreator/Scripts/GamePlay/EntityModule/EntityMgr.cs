using System;
using System.Collections;
using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public sealed partial class EntityMgr : IEntityMgr
    {
        internal Dictionary<int, IEntity> entityDict = new();

        internal List<IEntity> entityList = new();

        internal Dictionary<string, IEntityGroup> groupDict = new();

        internal List<IEntityGroup> groupList = new();

        internal Dictionary<int, IEntityGroup> entityGroupDict = new();

        public int entityCount => entityDict.Count;

        public int entityGroupDCount => groupDict.Count;

        //private EntityMgr() { }

        void IDearMgr.WorkWork()
        {
            entityDict ??= new();
            entityList ??= new();
            groupDict ??= new();
            groupList ??= new();
            entityGroupDict ??= new();
        }

        void IDearMgr.DoNothing()
        {
            ReleaseAllEntity();
            entityDict = null;
            entityList = null;
            groupDict = null;
            groupList = null;
            entityGroupDict = null;
        }

        void IDearMgr.UpdateMGR()
        {
            for (int i = 0; i < entityList.Count; i++)
            {
                if (entityList[i].enable)
                    entityList[i].UpdataEnt();
            }
        }

        void IDearMgr.FixedUpdateMGR()
        {
            for (int i = 0; i < entityList.Count; i++)
            {
                if (entityList[i].enable)
                    entityList[i].FixedUpdataEnt();
            }
        }

        void IDearMgr.LateUpdateMGR()
        {
            for (int i = 0; i < entityList.Count; i++)
            {
                if (entityList[i].enable)
                    entityList[i].LateUpdataEnt();
            }
        }


        //==================================================================================================================


        public bool IsValidEntity(int id)
        => GetEntity(id) != null;

        public bool IsValidEntity<T>(T ent) where T : IEntity
        => ent != null && GetEntity(ent.entID) != null;

        public IEntity CreateEntity(string entityName, string groupName, Type type,string config = null)
        {
            var obj = Mgr.GPool.Load(entityName, true, false);
            var entity = (IEntity)Mgr.RPool.Load(type);
            entity.InitEnt(entityName, obj, config);
            entityDict.Add(obj.GetInstanceID(), entity);
            entityList.Add( entity);
            if (!string.IsNullOrWhiteSpace(groupName)) SetGroup(groupName, entity);
            return entity;
        }

        public T CreateEntity<T>(string entityName, string groupName, string config) where T : IEntity, new()
        => (T)CreateEntity(entityName, groupName, typeof(T),config);

        public void ReleaseEntity(int id)
        {
            var entity = GetEntity(id);
            if (entity is null) return;
            RemoveEntityGroup(entity);
            entityList.Remove(entity);
            entityDict.Remove(entity.entID);
            Mgr.RPool.Release(entity);

        }

        public void ReleaseEntity<T>(T ent) where T : IEntity
        {
            if (ent is null) return;
            ReleaseEntity(ent.entID);
        }

        public void ShowEntity(int id)
        {
            GetEntity(id).enable = true;
        }

        public void HideEntity(int id)
        {
            GetEntity(id).enable = false;
        }

        public void ShowAllEntity()
        {
            for (int i = entityList.Count - 1; i >= 0; i--)
            {
                entityList[i].enable = true;
            }
        }

        public void HideAllEntity()
        {
            for (int i = entityList.Count - 1; i >= 0; i--)
            {
                entityList[i].enable = false;
            }
        }

        public void ReleaseAllEntity()
        {
            entityGroupDict.Clear();
            entityDict.Clear();
            groupDict.Clear();
            IEntityGroup entityGroup;
            for (int i = groupList.Count - 1; i >= 0; i--)
            {
                entityGroup = groupList[i];
                groupList.RemoveAt(i);
                Mgr.RPool.Release(entityGroup);
            }
            IEntity entity;
            for (int i = entityList.Count - 1; i >= 0; i--)
            {
                entity = entityList[i];
                entityList.RemoveAt(i);
                Mgr.RPool.Release(entity);
            }
        }

        public IEntity GetEntity(int id)
        => entityDict.TryGetValue(id, out var entity) ? entity : null;

        public List<IEntity> GetAllEntity(bool isCopy)
        => isCopy ? entityList.CopyToNewList() : entityList;


        //==================================================================================================================


        public bool HasGroup(string groupName)
        => groupDict.TryGetValue(groupName, out _);

        public void SetGroup(string groupName, IEntity entity)
        {
            if (string.IsNullOrWhiteSpace(groupName)) UCMDebug.LogError("无法设置【" + groupName + "】实体组");
            if (!IsValidEntity(entity.entID)) return;
            RemoveEntityGroup(entity);
            var group = GetGroup(groupName);
            if (group is null)
            {
                group = Mgr.RPool.Load<EntityGroup>();
                group.groupName = groupName;
                groupDict.Add(groupName, group);
                groupList.Insert(0, group);
            }
            group.AddEntity(entity);
            entityGroupDict.Add(entity.entID, group);
        }

        public void RemoveGroup(string groupName)
        {
            var group = GetGroup(groupName);
            if (group is null) return;
            group.ClearEntity();
            groupDict.Remove(groupName);
            groupList.Remove(group);
        }

        public void ShowGroup(string groupName)
        {
            GetGroup(groupName)?.ShowEntity();
        }

        public void HideGroup(string groupName)
        {
            GetGroup(groupName)?.HideEntity();
        }

        public void ClearGroup(string groupName)
        {
            GetGroup(groupName)?.ClearEntity();
        }

        public IEntityGroup GetGroup(string groupName)
        => groupDict.TryGetValue(groupName, out var value) ? value : null;

        public void RemoveEntityGroup<T>(T ent) where T : IEntity
        {
            if (!entityGroupDict.Remove(ent.entID, out var value)) return;
            value.RemoveEntity(ent);
        }

        public IEntityGroup GetEntityGroup<T>(T ent) where T : IEntity
        => entityGroupDict.TryGetValue(ent.entID, out var value) ? value : null;

        public List<IEntityGroup> GetAllEntityGroup(bool isCopy)
        => isCopy ? groupList.CopyToNewList() : groupList;
    }
}