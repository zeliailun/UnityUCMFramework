namespace UnknownCreator.Modules
{
    public class BTGoToHFSM : BTTaskState
    {
        public string nameFsm;
        public string nameState;

        private IStateMachine sm;

        protected override void OnEnd()
        {
            sm = null;
        }

        protected override BTStateType OnUpdate()
        {
            if (string.IsNullOrWhiteSpace(nameFsm) || string.IsNullOrWhiteSpace(nameState))
                return BTStateType.Failed;

            sm = cntlr.GetHBSM(nameFsm);
            if (sm is null || !sm.HasState(nameState)) return BTStateType.Failed;
            sm.ChangeState(nameState, false);
            return BTStateType.Succeed;
        }
    }
}