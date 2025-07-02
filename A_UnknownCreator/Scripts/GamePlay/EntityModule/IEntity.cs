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

        public string entClassName { get; }

        public int entID { get; }

        public bool enable { get; set; }

        void InitEnt(string entName, GameObject ent, string data);

        void UpdataEnt();

        void FixedUpdataEnt();

        void LateUpdataEnt();

        void ShowEnt();

        void HideEnt();

        public T As<T>() where T : class, IEntity
        => this as T;
    }
}