namespace UnknownCreator.Modules
{

    /// <summary>
    /// 移除技能前(不要在事件里重复移除会引起bug)
    /// </summary>
    public readonly struct EvtAbilityWillRemove : IBusEvent
    {
        public readonly AbilityBase ability;
        public readonly Unit owner;

        public EvtAbilityWillRemove(AbilityBase ability, Unit owner)
        {
            this.ability = ability;
            this.owner = owner;
        }
    }

}
