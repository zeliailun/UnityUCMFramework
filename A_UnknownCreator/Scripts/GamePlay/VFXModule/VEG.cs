using UnityEngine;
using UnityEngine.VFX;

namespace UnknownCreator.Modules
{
    public class VEG : VfxBase
    {
        private VisualEffect[] veArr;
        private VFXEventAttribute[] attrArr;

        public override void InitVfx(string vfxName, GameObject obj, IEntity owner)
        {
            base.InitVfx(vfxName, obj, owner);
            veArr = rootObj.GetComponentsInChildren<VisualEffect>();
            attrArr = new VFXEventAttribute[veArr.Length];
            VisualEffect ve;
            for (int i = 0; i < veArr.Length; i++)
            {
                ve = veArr[i];
                ve.Stop();
                attrArr[i] = ve.CreateVFXEventAttribute();
            }
            rootObj.SetActive(true);
        }


        public override void PlayVfx()
        {
            for (int i = 0; i < veArr.Length; i++)
            {
                veArr[i].Play(attrArr[i]);
            }
        }

        public void PlayVfx(string evtName = "OnPlay")
        {
            for (int i = 0; i < veArr.Length; i++)
            {
                veArr[i].SendEvent(evtName, attrArr[i]);
            }
        }

        public override void StopVfx()
        {
            for (int i = 0; i < veArr.Length; i++)
            {
                veArr[i].Stop(attrArr[i]);
            }
        }

        public void StopVfx(string evtName = "OnStop")
        {
            for (int i = 0; i < veArr.Length; i++)
            {
                veArr[i].SendEvent(evtName, attrArr[i]);
            }
        }

        public override void PauseVfx(bool isPause)
        {
            for (int i = 0; i < veArr.Length; i++)
            {
                veArr[i].pause = isPause;
            }
        }

        public VisualEffect GetVfx(int index)
        {
            return index < veArr.Length && index >= 0 ? veArr[index] : null;
        }
        public VFXEventAttribute GetVfxAttr(int index)
        {
            return index < attrArr.Length && index >= 0 ? attrArr[index] : null;
        }

        public VisualEffect[] GetAllVfx()
        {
            return veArr;
        }

        public VFXEventAttribute[] GetAllVfxAttr()
        {
            return attrArr;
        }

        public override void OnRelease()
        {
            veArr = null;
            attrArr = null;
        }
    }
}