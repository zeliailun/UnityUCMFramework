namespace UnknownCreator.Modules
{
    public struct AbilityCastAnimConfig
    {
        public string animPath;
        public float triggerTime;

        public AbilityCastAnimConfig(string animPath, float triggerTime)
        {
            this.animPath = animPath;
            this.triggerTime = triggerTime;
        }
    }
}