using System;

namespace UnknownCreator.Modules
{
    public sealed class EventHandle : IDisposable
    {
        private Action release;

        public bool IsDisposed => release == null;

        public EventHandle(Action release)
        {
            this.release = release;
        }

        public void Dispose()
        {
            if (release == null)
                return;

            Action temp = release;
            release = null;
            temp.Invoke();
        }
    }
}
