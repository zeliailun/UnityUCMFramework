namespace UnknownCreator.Modules
{
    public class BTParallelSelectorState : BTCompositeState
    {
        private int count;

        private BTStateType st;

        protected override void OnStart()
        {
            count = 0;
        }

        protected override BTStateType OnUpdate()
        {
            while (HasCurrentChild())
            {
                st = children[currentIndex].UpdateBT();
                if (st is BTStateType.Succeed) return BTStateType.Succeed;
                if (st is BTStateType.Failed or BTStateType.None) count++;
                if (count == childrenCount) return BTStateType.Failed;
                currentIndex++;
            }
            return BTStateType.Running;
        }
    }
}