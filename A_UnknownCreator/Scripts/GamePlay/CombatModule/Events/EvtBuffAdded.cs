namespace UnknownCreator.Modules
{
    /// <summary>
    /// 添加BUFF后
    /// </summary>
    public readonly struct EvtBuffAdded : IBusEvent
    {
        public readonly BuffBase buff;
        public readonly Unit owner;

        public EvtBuffAdded(BuffBase buff, Unit owner)
        {
            this.buff = buff;
            this.owner = owner;
        }
    }
}
