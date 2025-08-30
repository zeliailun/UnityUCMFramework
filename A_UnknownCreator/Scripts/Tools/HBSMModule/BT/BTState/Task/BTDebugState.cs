namespace UnknownCreator.Modules
{
    public enum LogType
    {
        Log,
        LogWarning,
        LogError
    }

    public class BTDebugState : BTTaskState
    {
        public LogType log = LogType.Log;

        public string text = "Hello World";

        protected override BTStateType OnUpdate()
        {
            switch (log)
            {
                case LogType.Log:
                    UCMDebug.Log(text);
                    break;
                case LogType.LogWarning:
                    UCMDebug.LogWarning(text);
                    break;
                case LogType.LogError:
                    UCMDebug.LogError(text);
                    break;
            }

            return BTStateType.Succeed;
        }
    }
}

