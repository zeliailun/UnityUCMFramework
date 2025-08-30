namespace UnknownCreator.Modules
{
    public readonly struct EvtAbilityChargeChanged
    {
        public readonly Unit owner;
        public readonly AbilityBase ability;
        public readonly int value;

        public EvtAbilityChargeChanged(AbilityBase ability, Unit owner, int value)
        {
            this.ability = ability;
            this.owner = owner;
            this.value = value;
        }
    }
}