namespace UnknownCreator.Modules
{
    public class Variable<T> : IVariable, IReference
    {
        public string key { get; private set; }
        public T value { get; private set; }

        public Variable()
        {

        }

        public void Init(string key, T value)
        {
            this.key = key;
            this.value = value;
        }

        public void ReplaceValue(T value)
        {
            this.value = value;
        }

        public IVariable Copy()
        {
            var v = Mgr.RPool.Load<Variable<T>>();
            v.Init(this.key, this.value);
            return v;
        }

        void IReference.ObjRelease() {
            key = null;
            value = default; 
        }

    }
}
