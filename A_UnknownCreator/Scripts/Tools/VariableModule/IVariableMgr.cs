namespace UnknownCreator.Modules
{
    public interface IVariableMgr : IDearMgr
    {
        int variableCount { get; }
        bool HasVariable(string key);
        IVariable AddVariable(string key, IVariable variable);
        T AddVariable<T>(string key) where T : class, IVariable, new();
        IVariable GetVariable(string key);
        T GetVariable<T>(string key) where T : class, IVariable, new();
        bool RemoveVariable(string key);
        IVariable AddValue<T>(string key, T t);
        IVariable AddValue<T>(T t);
        T GetValue<T>(string key);
        T GetValue<T>();
        void ReplaceValue<T>(string key, T t);
        void ClearAll();
    }
}