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

            veArr = rootObj != null
                ? rootObj.GetComponentsInChildren<VisualEffect>(true)
                : null;

            if (veArr == null || veArr.Length == 0)
            {
                attrArr = null;
                if (rootObj != null)
                    rootObj.SetActive(true);
                return;
            }

            attrArr = new VFXEventAttribute[veArr.Length];

            for (int i = 0; i < veArr.Length; i++)
            {
                VisualEffect ve = veArr[i];
                if (ve == null) continue;

                ve.Stop();
                attrArr[i] = ve.CreateVFXEventAttribute();
            }

            rootObj.SetActive(true);
        }

        public override void PlayVfx()
        {
            if (!CanUseVfx()) return;

            for (int i = 0; i < veArr.Length; i++)
            {
                if (veArr[i] != null)
                    veArr[i].Play(attrArr[i]);
            }
        }

        public void PlayVfx(string evtName = "OnPlay")
        {
            if (!CanUseVfx()) return;

            for (int i = 0; i < veArr.Length; i++)
            {
                if (veArr[i] != null)
                    veArr[i].SendEvent(evtName, attrArr[i]);
            }
        }

        public override void StopVfx()
        {
            if (!CanUseVfx()) return;

            for (int i = 0; i < veArr.Length; i++)
            {
                if (veArr[i] != null)
                    veArr[i].Stop(attrArr[i]);
            }
        }

        public void StopVfx(string evtName = "OnStop")
        {
            if (!CanUseVfx()) return;

            for (int i = 0; i < veArr.Length; i++)
            {
                if (veArr[i] != null)
                    veArr[i].SendEvent(evtName, attrArr[i]);
            }
        }

        public override void PauseVfx(bool isPause)
        {
            if (isRelease || veArr == null) return;

            for (int i = 0; i < veArr.Length; i++)
            {
                if (veArr[i] != null)
                    veArr[i].pause = isPause;
            }
        }

        public VisualEffect GetVfx(int index)
        {
            return veArr != null && index >= 0 && index < veArr.Length
                ? veArr[index]
                : null;
        }

        public VFXEventAttribute GetVfxAttr(int index)
        {
            return attrArr != null && index >= 0 && index < attrArr.Length
                ? attrArr[index]
                : null;
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
            if (veArr != null)
            {
                for (int i = 0; i < veArr.Length; i++)
                {
                    if (veArr[i] == null) continue;

                    veArr[i].pause = false;
                    veArr[i].Stop();
                }
            }

            veArr = null;
            attrArr = null;
        }

        private bool CanUseVfx()
        {
            return !isRelease && veArr != null && attrArr != null;
        }
    }
}
