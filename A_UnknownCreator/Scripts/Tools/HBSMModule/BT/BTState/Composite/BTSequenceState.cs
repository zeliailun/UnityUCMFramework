namespace UnknownCreator.Modules
{
    public class BTSequenceState : BTCompositeState
    {
        private BTStateType st;

        protected override BTStateType OnUpdate()
        {
            st = BTStateType.Succeed;
            while (HasCurrentChild())
            {
                st = children[currentIndex].UpdateBT();
                if (st is BTStateType.Succeed or BTStateType.None)
                    currentIndex++;
                else
                    break;
            }
            return st;
        }
    }
}