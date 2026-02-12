using UnityEngine;

namespace UnknownCreator.Modules
{
    public interface IEntity : IReference
    {
        public IHBSMController hbsm { get; }

        public GameObject ent { get; }

        public Transform entT { get; }

        public Vector3 entP { get; }

        public Quaternion entR { get; }

        public string entName { get; }

        public EntityId entID { get; }

        public bool enable { get; set; }

        void UpdataEnt();

        void FixedUpdataEnt();

        void LateUpdataEnt();

        void ShowEnt();

        void HideEnt();

        void AddBodyPart(int id, string path);

        void RemoveBodyPart(int id);

        Transform GetBodyPart(int id);

        void ClearBodyPart();

        public T As<T>() where T : class, IEntity
        => this as T;
    }
}