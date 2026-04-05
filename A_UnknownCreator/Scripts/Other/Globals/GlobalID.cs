using System.Threading;

namespace UnknownCreator.Modules
{
    public static class GlobalID
    {
        // 使用 long，避免溢出问题
        private static long currentId = 0;

        /// <summary>
        /// 获取全局唯一ID（线程安全）
        /// </summary>
        public static long GetUniqueID()
        {
            return Interlocked.Increment(ref currentId);
        }

        /// <summary>
        /// 重置ID（仅用于重新开局/清档）
        /// </summary>
        public static void ResetID()
        {
            Interlocked.Exchange(ref currentId, 0);
        }
    }
}