namespace UnknownCreator.Modules
{
    public interface IDearMgr
    {
        int Priority() => 0;

        void WorkWork() { }

        void DoNothing() { }

        void UpdateMGR() { }

        void LateUpdateMGR() { }

        void FixedUpdateMGR() { }
    }
}
