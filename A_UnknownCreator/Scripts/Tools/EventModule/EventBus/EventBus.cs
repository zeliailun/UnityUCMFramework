using System.Collections.Generic;
using System.Text;

namespace UnknownCreator.Modules
{
    /// <summary>
    /// 所有 Bus<TEvent> 的事件数据实现它。
    /// </summary>
    public interface IBusEvent { }

    internal interface IBusControl
    {
        void ClearAll();
        int ListenerCount { get; }
        string DebugName { get; }
    }

    public static class EventBus
    {
        private static readonly List<IBusControl> controls = new();

        /// <summary>
        /// 为 true 时，所有 Bus / QueryBus 都不会派发。
        /// </summary>
        public static bool Interrupt { get; set; }

        internal static void Register(IBusControl control)
        {
            if (control == null)
                return;

            if (!controls.Contains(control))
                controls.Add(control);
        }

        public static void ClearAll()
        {
            for (int i = 0; i < controls.Count; i++)
            {
                controls[i].ClearAll();
            }

            Interrupt = false;
        }

#if UNITY_EDITOR
        public static void DebugDump()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("========== EventBus Debug Dump ==========");

            for (int i = 0; i < controls.Count; i++)
            {
                IBusControl control = controls[i];

                if (control.ListenerCount <= 0)
                    continue;

                builder.AppendLine($"{control.DebugName} : {control.ListenerCount}");
            }

            builder.AppendLine("=========================================");

            UCMDebug.Log(builder.ToString());
        }
#endif
    }
}
