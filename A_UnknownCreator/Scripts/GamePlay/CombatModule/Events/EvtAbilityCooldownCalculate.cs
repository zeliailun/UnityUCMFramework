namespace UnknownCreator.Modules
{
    public readonly struct EvtAbilityCooldownCalculate : IBusEvent
    {
        public readonly AbilityBase ability;
        public readonly Unit owner;
        public readonly double currentCooldown;

        public EvtAbilityCooldownCalculate(AbilityBase ability, Unit owner, double currentCooldown)
        {
            this.ability = ability;
            this.owner = owner;
            this.currentCooldown = currentCooldown;
        }
    }

    
}
