using UnknownCreator.Modules;

public struct TimerHandle<T> where T : class, ITimer
{
    private long id;
    private T cache;

    public long idValue => id;

    public bool isEmpty => id == TimerGlobals.InvalidTimerID;

    public bool isValid =>
        cache != null &&
        cache.isInited &&
        cache.id == id;

    public TimerHandle(T timer)
    {
        cache = timer;
        id = timer != null ? timer.id : TimerGlobals.InvalidTimerID;
    }

    public bool TryGet(out T timer)
    {
        if (isValid)
        {
            timer = cache;
            return true;
        }

        timer = null;
        Clear();
        return false;
    }

    public void Destroy()
    {
        if (id != TimerGlobals.InvalidTimerID)
        {
            Mgr.Timer.RemoveTimer(id);
        }

        Clear();
    }

    public void Clear()
    {
        id = TimerGlobals.InvalidTimerID;
        cache = null;
    }
}