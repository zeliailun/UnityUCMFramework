using System;
using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public class DefaultGodHelper : IGodHelper
    {
        private Dictionary<Type, IDearMgr> mgrDict = new();

        private List<IDearMgr> mgrList = new();

        private HashSet<Type> notDelHashSet = new();

        private Type mgrType;

        public IDearMgr AddMgr(IDearMgr mgr, bool notRemove)
        {
            mgrType = mgr.GetType();
            if (!mgrDict.TryGetValue(mgrType, out var result))
            {
                mgr.WorkWork();
                mgrList.Add( mgr);
                mgrDict.Add(mgrType, mgr);
                if (notRemove) notDelHashSet.Add(mgrType);
                return mgr;
            }
            return result;
        }

        public T AddMgr<T>(bool notRemove) where T : IDearMgr, new()
        => (T)AddMgr(new T(), notRemove);

        public void RemoveMgr(Type type)
        {
            if (mgrDict.TryGetValue(type, out var mgr) && !notDelHashSet.Contains(type))
            {
                mgr.DoNothing();
                mgrList.Remove(mgr);
                mgrDict.Remove(type);
            }
        }

        public void RemoveMgr<T>() where T : IDearMgr
        => RemoveMgr(typeof(T));

        public IDearMgr GetMgr(Type type)
        => mgrDict.TryGetValue(type, out var cache) ? cache : default;

        public T GetMgr<T>() where T : IDearMgr, new()
        => (T)GetMgr(typeof(T));

        public List<IDearMgr> GetMgrList()
        => mgrList.CopyToNewList();

        public void RemoveAllMgr()
        {
            mgrDict.Clear();
            notDelHashSet.Clear();
            for (int i = mgrList.Count - 1; i >= 0; i--)
            {
                mgrList[i].DoNothing();
                mgrList.RemoveAt(i);
            }
            mgrType = null;
        }

        public void SortMgr()
        {
            mgrList.Sort((x, y) => x.Priority().CompareTo(y.Priority()));
        }

        public void Update()
        {
            for (int i = 0; i < mgrList.Count; i++)
            {
                mgrList[i]?.UpdateMGR();
            }
        }

        public void FixedUpdate()
        {
            for (int i = 0; i < mgrList.Count; i++)
            {
                mgrList[i]?.FixedUpdateMGR();
            }
        }

        public void LateUpdate()
        {
            for (int i = 0; i < mgrList.Count; i++)
            {
                mgrList[i]?.LateUpdateMGR();
            }
        }
    }
}