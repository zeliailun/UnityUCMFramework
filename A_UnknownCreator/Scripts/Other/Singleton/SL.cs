namespace UnknownCreator.Modules
{

    public abstract class SL<T> where T : class, new()
    {
        public readonly static T i = new();

        protected SL() { }
    }
}
