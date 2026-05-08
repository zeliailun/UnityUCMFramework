namespace UnknownCreator.Modules
{
    public struct EvtReturnUnitModelName : IBusEvent
    {
        public string modelName;

        public EvtReturnUnitModelName(string modelName)
        {
            this.modelName = modelName;
        }
    }
}