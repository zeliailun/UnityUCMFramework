namespace UnknownCreator.Modules
{
    public readonly struct EvtAbilityCooldownStart
    {
        public readonly AbilityBase ability;
        public readonly Unit owner;
        public readonly double oldCooldown;
        public readonly double newCooldown;

        public EvtAbilityCooldownStart(AbilityBase ability, Unit owner, double oldCooldown, double newCooldown)
        {
            this.ability = ability;
            this.owner = owner;
            this.oldCooldown = oldCooldown;
            this.newCooldown = newCooldown;
        }
    }
}
