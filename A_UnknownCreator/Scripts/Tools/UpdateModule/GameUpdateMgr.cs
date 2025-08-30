using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public sealed class GameUpdateMgr : IUpdateMgr
    {
        private List<IOnUpdate> updataList = new();

        private List<IOnFixedUpdate> fixedUpdataList = new();

        private List<IOnLateUpdate> lateUpdateList = new();

        public int GetUpdateCount => updataList.Count;
        public int GetFixedUpdateCount => fixedUpdataList.Count;
        public int GetLateUpdateCount => lateUpdateList.Count;

        //private GameUpdateMgr() { }

        void IDearMgr.WorkWork()
        {
            updataList ??= new();
            fixedUpdataList ??= new();
            lateUpdateList ??= new();
        }

        void IDearMgr.DoNothing()
        {
            updataList.Clear();
            fixedUpdataList.Clear();
            lateUpdateList.Clear();
            updataList = null;
            fixedUpdataList = null;
            lateUpdateList = null;
        }

        void IDearMgr.UpdateMGR()
        {
            for (int i = 0; i < updataList.Count; i++)
            {
                updataList[i]?.OnUpdate();
            }
        }

        void IDearMgr.FixedUpdateMGR()
        {
            for (int i = 0; i < fixedUpdataList.Count; i++)
            {
                fixedUpdataList[i]?.OnFixedUpdate();
            }
        }

        void IDearMgr.LateUpdateMGR()
        {
            for (int i = 0; i < lateUpdateList.Count; i++)
            {
                lateUpdateList[i]?.OnLateUpdate();
            }
        }

        public void AddUpdata<T>(T eup) where T : IOnUpdate
        {
            if (eup is not null && !updataList.Contains(eup))
                updataList.Add(eup);
        }

        public void AddFixedUpdata<T>(T eup) where T : IOnFixedUpdate
        {
            if (eup is not null && !fixedUpdataList.Contains(eup))
                fixedUpdataList.Add( eup);
        }

        public void AddLateUpdata<T>(T eup) where T : IOnLateUpdate
        {
            if (eup is not null && !lateUpdateList.Contains(eup))
                lateUpdateList.Add( eup);
        }

        public void RemoveUpdata<T>(T eup) where T : IOnUpdate
        {
            updataList.Remove(eup);
        }

        public void RemoveFixedUpdata<T>(T eup) where T : IOnFixedUpdate
        {
            fixedUpdataList.Remove(eup);
        }

        public void RemoveLateUpdata<T>(T eup) where T : IOnLateUpdate
        {
            lateUpdateList.Remove(eup);
        }

    }
}
