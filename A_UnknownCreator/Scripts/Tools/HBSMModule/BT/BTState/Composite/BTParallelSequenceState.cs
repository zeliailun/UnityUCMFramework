namespace UnknownCreator.Modules
{
    public class BTParallelSequenceState : BTCompositeState
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
                if (st is BTStateType.Failed) return BTStateType.Failed;
                if (st is BTStateType.Succeed or BTStateType.None) count++;
                if (count == childrenCount) return BTStateType.Succeed;
                currentIndex++;
            }
            return BTStateType.Running;
        }
    }
}