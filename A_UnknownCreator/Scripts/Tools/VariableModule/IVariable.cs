namespace UnknownCreator.Modules
{
    public interface IVariable
    {
        string key { get; }

        IVariable Copy();
    }
}