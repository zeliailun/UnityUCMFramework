namespace UnknownCreator.Modules
{
    /// <summary>
    /// 移除BUFF前(不要在事件里重复移除会引起bug)
    /// </summary>
    public readonly struct EvtBuffWillRemove : IBusEvent
    {
        public readonly BuffBase buff;
        public readonly Unit owner;

        public EvtBuffWillRemove(BuffBase buff, Unit owner)
        {
            this.buff = buff;
            this.owner = owner;
        }
    }
}
