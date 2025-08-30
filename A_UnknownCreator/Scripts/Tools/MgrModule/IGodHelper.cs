using System;
using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public interface IGodHelper
    {
        IDearMgr AddMgr(IDearMgr mgr, bool notRemove);
        T AddMgr<T>(bool notRemove) where T : IDearMgr, new();
        void RemoveMgr(Type type);
        void RemoveMgr<T>() where T : IDearMgr;
        IDearMgr GetMgr(Type type);
        T GetMgr<T>() where T : IDearMgr, new();
        List<IDearMgr> GetMgrList();

        void SortMgr();
        void RemoveAllMgr();
        void Update();
        void FixedUpdate();
        void LateUpdate();
    }
}