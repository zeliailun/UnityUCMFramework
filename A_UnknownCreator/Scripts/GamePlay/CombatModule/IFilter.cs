namespace UnknownCreator.Modules
{
    public interface IFilter<in TContext, out TResult>
    {
        TResult Invoke(TContext context);

        void OnSet();

        void OnClear();
    }
}