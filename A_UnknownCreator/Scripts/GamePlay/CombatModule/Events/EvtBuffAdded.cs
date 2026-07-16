namespace UnknownCreator.Modules
{
    /// <summary>
    /// 添加BUFF后
    /// </summary>
    public readonly struct EvtBuffAdded : IBusEvent
    {
        public readonly BuffBase buff;
        public readonly Unit target;

        public EvtBuffAdded(BuffBase buff, Unit target)
        {
            this.buff = buff;
            this.target = target;
        }
    }
}
