using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public class UITKBuilder : UITKMonoBase
    {

        [field: SerializeField]
        public string idName { get; private set; }

        public HBSMController hbsm { get; private set; }

        [SerializeReference, ShowSerializeReference]
        public List<IHBSMBuilder> builder = new();



        public override void OnUpdate()
        {
            hbsm.UpdateAllHBSM();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (hbsm is null)
            {

                hbsm = new();
                hbsm.kv.AddValue(uid);
                foreach (var item in builder)
                {
                    if (item != null)
                        hbsm.Create(item);
                }

            }


            UITKMgr.AddUID(this);
            hbsm.EnableAllHBSM();


        }

        protected override void OnDisable()
        {
            UITKMgr.RemoveUID(this);
            hbsm.DisableAllHBSM();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            hbsm.ReleaseAllHBSM();
            hbsm = null;
        }

    }
}