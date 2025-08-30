namespace UnknownCreator.Modules
{
    public class BTInvertState : BTDecoratorState
    {
        private BTStateType st;

        protected override BTStateType OnUpdate()
        {
            if (!HasChild()) return BTStateType.None;
            st = child.UpdateBT();
            return st switch
            {
                BTStateType.Succeed => BTStateType.Failed,
                BTStateType.Failed => BTStateType.Succeed,
                _ => st,
            };
        }
    }
}