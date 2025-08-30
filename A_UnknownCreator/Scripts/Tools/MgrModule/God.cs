using System;
using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public static class God
    {
        private static IGodHelper god = new DefaultGodHelper();

        public static void SetGod(IGodHelper value)
        {
            god.RemoveAllMgr();
            god = value;
        }

        public static IDearMgr AddMgr(IDearMgr mgr, bool notRemove)
        => god.AddMgr(mgr, notRemove);

        public static T AddMgr<T>(bool notRemove) where T : IDearMgr, new()
        => god.AddMgr<T>(notRemove);

        public static void RemoveMgr(Type type)
        => god.RemoveMgr(type);

        public static void RemoveMgr<T>() where T : IDearMgr
        => god.RemoveMgr<T>();

        public static IDearMgr GetMgr(Type type)
        => god.GetMgr(type);

        public static T GetMgr<T>() where T : IDearMgr, new()
        => god.GetMgr<T>();

        public static List<IDearMgr> GetMgrList()
        => god.GetMgrList();

        public static void RemoveAllMgr()
        => god.RemoveAllMgr();

        public static void SortMgr()
        {
            god.SortMgr();
        }

        public static void Update()
        {
            god.Update();
        }

        public static void FixedUpdate()
        {
            god.FixedUpdate();
        }

        public static void LateUpdate()
        {
            god.LateUpdate();
        }
    }
}

