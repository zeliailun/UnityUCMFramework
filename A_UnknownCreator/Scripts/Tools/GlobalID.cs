using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public static class GlobalID
    {
        private static HashSet<long> idHashSet = new();

        private static Stack<long> idStack = new();

        private static long currentId = 1;

        private static readonly object idLock = new();

        public static int idCount
        {
            get
            {
                lock (idLock)
                {
                    return idStack.Count;
                }
            }
        }

        public static long GetUniqueID()
        {
            lock (idLock)
            {
                long id;
                if (idCount > 0)
                {
                    id = idStack.Pop();
                    idHashSet.Remove(id);
                }
                else
                {
                    if (currentId == long.MaxValue)
                    {
                        throw new CustomException("ID超出最大值");
                    }
                    id = currentId;
                    ++currentId;
                }
                return id;
            }
        }

        public static void RecycleID(long id)
        {
            lock (idLock)
            {
                if (idHashSet.Contains(id))
                {
                    throw new CustomException("尝试回收重复ID");
                }
                else
                {
                    idStack.Push(id);
                    idHashSet.Add(id);
                }
            }
        }

        public static void ResetID()
        {
            lock (idLock)
            {
                idStack.Clear();
                idHashSet.Clear();
                currentId = 1;
            }
        }
    }
}