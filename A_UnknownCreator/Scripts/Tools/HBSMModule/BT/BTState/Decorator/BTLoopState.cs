namespace UnknownCreator.Modules
{
    public class BTLoopState : BTDecoratorState
    {
        protected override BTStateType OnUpdate()
        {
            if (!HasChild()) return BTStateType.Failed;
            child.UpdateBT();
            return BTStateType.Running;
        }
    }
}