namespace UnknownCreator.Modules
{
    public class Variable<T> : IVariable, IReference
    {
        public T value { get; private set; }

        public Variable()
        {

        }

        public void Init(T value)
        {
            this.value = value;
        }

        public void ReplaceValue(T value)
        {
            this.value = value;
        }

        void IReference.ObjRelease() { value = default; }
    }
}
