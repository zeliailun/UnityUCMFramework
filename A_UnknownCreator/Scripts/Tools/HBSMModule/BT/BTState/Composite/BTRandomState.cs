namespace UnknownCreator.Modules
{
    public class BTRandomState : BTCompositeState
    {
        private BTStateBase node;

        protected override void OnStart()
        => node = !HasChild() ? null : children[UnityEngine.Random.Range(0, childrenCount)];

        protected override BTStateType OnUpdate()
        => node is null ? BTStateType.Failed : node.UpdateBT();
    }
}