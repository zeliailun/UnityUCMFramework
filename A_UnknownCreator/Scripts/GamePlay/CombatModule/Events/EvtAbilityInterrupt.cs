namespace UnknownCreator.Modules
{
    public readonly struct EvtAbilityInterrupt
    {
        public readonly AbilityBase ability;
        public readonly Unit owner;
        public readonly bool isPointOrBackswing;

        public EvtAbilityInterrupt(AbilityBase ability, Unit owner, bool isPointOrBackswing)
        {
            this.ability = ability;
            this.owner = owner;
            this.isPointOrBackswing = isPointOrBackswing;
        }
    }
}