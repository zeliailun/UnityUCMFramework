namespace UnknownCreator.Modules
{
    public interface IState
    {
        public string stateName { get; }

        public IHBSMController cntlr { get; }

        public IStateMachine parent { get; }

        /// <summary>
        /// 内部调用
        /// </summary>
        /// <param name="stateName"></param>
        /// <param name="cntlr"></param>
        /// <param name="parent"></param>
        void Init(string stateName, IHBSMController cntlr, IStateMachine parent);

        void Enter();

        void Exit();

        void Update();

        void FixedUpdate();

        void LateUpdate();

        bool CanEnter();

        bool CanExit();

        bool CanSetDefault();
    }
}