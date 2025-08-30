namespace UnknownCreator.Modules
{
    public interface IReference
    {
        void ObjRestart() { }

        void ObjRelease() { }

        void ObjDestroy() { }
        void ObjPreload() { }
    }
}