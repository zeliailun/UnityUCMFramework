namespace UnknownCreator.Modules
{
    public static class EntityGlobals
    {
        public static bool IsValid(this IEntity entity)
        => entity != null && !Mgr.RPool.HasObject(entity);

    }
}