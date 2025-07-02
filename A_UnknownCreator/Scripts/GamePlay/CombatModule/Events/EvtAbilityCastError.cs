namespace UnknownCreator.Modules
{
    public readonly struct EvtAbilityCastError
    {
        public readonly AbilityBase ability;
        public readonly Unit owner;
        public readonly int error;

        public EvtAbilityCastError(AbilityBase ability, Unit owner, int error)
        {
            this.ability = ability;
            this.owner = owner;
            this.error = error;
        }
    }
}