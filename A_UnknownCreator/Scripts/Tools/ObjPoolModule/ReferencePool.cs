using System;

namespace UnknownCreator.Modules
{
    public class ReferencePool : ObjPoolBase<object>
    {
        public override string poolName => type?.Name;

        private Type type;

        public ReferencePool(ObjPoolInfo info, Type type)
        : base(info)
        {
            this.type = type;
        }


        protected override object OnCreate()
        {
            object obj = Activator.CreateInstance(type);
            if (obj is IReference @base) @base.ObjRestart();
            return obj;
        }


        protected override void OnPop(object obj)
        {
            if (obj is IReference @base1)
                @base1.ObjRestart();
        }

        protected override void OnPreStored(object obj)
        {
            if (obj is IReference @base)
                @base.ObjPreload();
        }


        protected override void OnRelease(object obj)
        {
            if (obj is IReference @base)
                @base.ObjRelease();
        }

        protected override void OnClear(object obj)
        {
            if (obj is IReference @base)
                @base.ObjDestroy();
        }

        protected override void OnPoolDestroy()
        {
            type = null;
        }


    }
}
