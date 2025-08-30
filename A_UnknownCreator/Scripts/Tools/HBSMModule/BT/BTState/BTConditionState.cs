namespace UnknownCreator.Modules
{
    public abstract class BTConditionState : BTDecoratorState
    {
        protected sealed override BTStateType OnUpdate()
        => GetCondition() ? (HasChild() ? child.UpdateBT() : BTStateType.Succeed) : BTStateType.Failed;

        protected abstract bool GetCondition();
    }
}
