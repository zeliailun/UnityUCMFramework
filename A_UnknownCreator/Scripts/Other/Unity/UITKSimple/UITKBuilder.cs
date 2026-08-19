using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    public class UITKBuilder : UITKMonoBase
    {
        [field: SerializeField]
        public string idName { get; private set; }

        public VisualElement root { get; private set; }

        public HBSMController hbsm { get; private set; }

        [SerializeReference, ShowSerializeReference]
        public List<IHBSMBuilder> builder = new();

        public override void OnUpdate()
        {
            hbsm?.UpdateAllHBSM();
        }

        protected override void OnDisable()
        {
            hbsm.DisableAllHBSM();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {

            UITKMgr.RemoveBuilder(this);
            hbsm.ReleaseAllHBSM();
            hbsm = null;
            root = null;
        }

        protected override void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
        {
            this.root = rootElement;

            if (hbsm is null)
            {
                hbsm = new();
                hbsm.kv.AddValue(nameof(UITKBuilder), this);
                hbsm.kv.AddValue(nameof(VisualElement), root);
                foreach (var item in builder)
                    if (item != null) hbsm.Create(item);
                UITKMgr.AddBuilder(this);
                hbsm.EnableAllHBSM();

                UITKMgr.OnUIReload?.Invoke(this);
            }else
            {

                
                UCMDebug.Log(idName + "刷新UI");

                hbsm.kv.ReplaceValue(nameof(UITKBuilder), this);
                hbsm.kv.ReplaceValue(nameof(VisualElement), root);
                hbsm.RefreshAllHBSM();

            
            }


        }
    }
}

