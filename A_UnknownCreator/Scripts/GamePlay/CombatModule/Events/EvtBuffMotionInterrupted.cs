namespace UnknownCreator.Modules
{
    internal readonly struct EvtBuffMotionInterrupted : IBusEvent
    {
        public readonly BuffBase buff;

        public EvtBuffMotionInterrupted(BuffBase buff)
        {
            this.buff = buff;
        }
    }
}
