using UnityEngine;
namespace UnknownCreator.Modules
{
    /*
    public interface IEntityGroup
    {
        object data { get; }

        GameObject ent { get; }

        int entID { get; }

        string entName { get; }

        string entClassName { get; }

        Transform entT { get; }

        bool isEnable { get; }

        void InitEnt(string entName, string config, GameObject ent, object data);
        void ShowEnt();
        void HideEnt();
        void UpdataEnt();
    }                */

    public interface IEntityGroup
    {
        string groupName { set; get; }
        int entityCount { get; }
        bool HasEntity(IEntity ent);
        void AddEntity(IEntity ent);
        void RemoveEntity(IEntity ent);
        void ShowEntity();
        void HideEntity();
        void ClearEntity();
    }






}