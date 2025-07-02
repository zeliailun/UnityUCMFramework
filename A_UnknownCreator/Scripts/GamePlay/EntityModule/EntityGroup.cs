using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public class EntityGroup : IEntityGroup, IReference
    {
        private List<IEntity> entList = new();

        public int entityCount => entList.Count;

        private string name;
        public string groupName
        {
            get => name;
            set
            {
                if (string.IsNullOrWhiteSpace(name))
                    name = value;
            }
        }

        public void AddEntity(IEntity ent)
        {
            if (!HasEntity(ent))
            {
                entList.Add(ent);
            }
        }

        public void RemoveEntity(IEntity ent)
        {
            if (HasEntity(ent))
            {
                entList.Remove(ent);
            }
        }

        public bool HasEntity(IEntity ent)
        => entList.Contains(ent);

        public void ShowEntity()
        {
            for (int i = entList.Count - 1; i >= 0; i--)
            {
                entList[i].enable = true;
            }
        }

        public void HideEntity()
        {
            for (int i = entList.Count - 1; i >= 0; i--)
            {
                entList[i].enable = false;
            }
        }

        public void ClearEntity()
        {
            for (int i = entList.Count - 1; i >= 0; i--)
                Mgr.Ent.RemoveEntityGroup(entList[i]);
        }

        void IReference.ObjRelease()
        {
            name = string.Empty;
            ClearEntity();
        }
    }
}