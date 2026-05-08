namespace UnknownCreator.Modules
{
    /// <summary>
    /// 不要在事件里重复移除会引起bug
    /// </summary>
    public readonly struct EvtTalentChanged:IBusEvent
    {
        public readonly Unit owner;
        public readonly AbilityBase talent;
        public readonly bool addOrRemove;

        public EvtTalentChanged(Unit owner, AbilityBase talent, bool addOrRemove)
        {
            this.owner = owner;
            this.talent = talent;
            this.addOrRemove = addOrRemove;
        }
    }
}
