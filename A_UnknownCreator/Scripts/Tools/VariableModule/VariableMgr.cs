using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public class VariableMgr : IReference, IVariableMgr
    {
        internal Dictionary<string, IVariable> varDict = new();

        internal List<IVariable> varList = new();

        public int variableCount => varDict.Count;

        void IDearMgr.WorkWork()
        {
            varDict ??= new();
            varList ??= new();
        }

        void IDearMgr.DoNothing()
        {
            ClearAll();
            varDict = null;
            varList = null;
        }

        public bool HasVariable(string key)
        => varDict.TryGetValue(key, out _);

        public IVariable AddVariable(string key, IVariable variable)
        {
            if (!varDict.TryGetValue(key, out var result))
            {
                varDict.Add(key, variable);
                return variable;
            }
            else
            {
                return result;
            }
        }

        public T AddVariable<T>(string key) where T : class, IVariable, new()
        {
            if (!varDict.TryGetValue(key, out var result))
            {
                var value = Mgr.RPool.Load<T>();
                varDict.Add(key, value);
                return value;
            }
            else
            {
                return (T)result;
            }
        }

        public IVariable GetVariable(string key)
        => varDict.TryGetValue(key, out var result) ? result : default;

        public T GetVariable<T>(string key) where T : class, IVariable, new()
        => (T)GetVariable(key);

        public bool RemoveVariable(string key)
        {
            if (varDict.Remove(key, out var value))
            {
                varList.Remove(value);
                Mgr.RPool.Release(value);
                return true;
            }
            return false;
        }

        public IVariable AddValue<T>(string key, T t)
        {
            if (!varDict.TryGetValue(key, out _))
            {
                var obj = Mgr.RPool.Load<Variable<T>>();
                obj.Init(t);
                varDict.Add(key, obj);
                varList.Add(obj);
                return obj;
            }
            return null;
        }

        public IVariable AddValue<T>(T t)
        {
          return  AddValue<T>(typeof(T).Name, t);
        }

        public T GetValue<T>(string key)
        => varDict.TryGetValue(key, out var result) ? ((Variable<T>)result).value : default;

        public T GetValue<T>()
        => GetValue<T>(typeof(T).Name);

        public void ReplaceValue<T>(string key, T t)
        {
            if (varDict.TryGetValue(string.IsNullOrWhiteSpace(key) ? typeof(T).Name : key, out var result))
                ((Variable<T>)result).ReplaceValue(t);
            else
                AddValue<T>(key, t);
        }

        public void ClearAll()
        {
            varDict.Clear();
            IVariable value;
            for (int i = varList.Count - 1; i >= 0; i--)
            {
                value = varList[i];
                if (varList.Remove(value))
                    Mgr.RPool.Release(value);
            }
        }

        void IReference.ObjRelease()
        {
            ClearAll();
        }


    }
}