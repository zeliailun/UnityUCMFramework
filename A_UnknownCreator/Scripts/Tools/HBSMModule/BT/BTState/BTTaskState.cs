namespace UnknownCreator.Modules
{
    public abstract class BTTaskState : BTStateBase
    {
        public override void Exit()
        {
            if (isStarted)
            {
                isStarted = false;
                OnEnd();
            }
        }

    }
}