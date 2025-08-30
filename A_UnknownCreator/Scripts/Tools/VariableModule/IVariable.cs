namespace UnknownCreator.Modules
{
    public interface IVariable
    {
        public void Init<T>(T value) { }

        public void ReplaceValue<T>(T value) { }
    }
}   