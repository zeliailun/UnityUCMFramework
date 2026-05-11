namespace UnknownCreator.Modules
{

    public sealed class FilterSlot<TContext, TResult>
    {
        private IFilter<TContext, TResult> filter;

        public bool hasFilter => filter != null;

        public TResult Invoke(TContext context)
        {
            return filter is null ? default : filter.Invoke(context);
        }

        public void Set(IFilter<TContext, TResult> newFilter)
        {
            if (filter == newFilter)
                return;

            filter?.OnClear();

            filter = newFilter;

            filter?.OnSet();
        }

        public void Clear()
        {
            if (filter == null)
                return;

            filter.OnClear();
            filter = null;
        }
    }
}