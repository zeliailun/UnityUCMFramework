namespace UnknownCreator.Modules
{
    public class BTSelectorState : BTCompositeState
    {
        private BTStateType st;

        protected override BTStateType OnUpdate()
        {
            st = BTStateType.Succeed;
            while (HasCurrentChild())
            {
                st = children[currentIndex].UpdateBT();
                if (st is BTStateType.Failed or BTStateType.None)
                    currentIndex++;
                else
                    break;
            }
            return st;
        }
    }
}
