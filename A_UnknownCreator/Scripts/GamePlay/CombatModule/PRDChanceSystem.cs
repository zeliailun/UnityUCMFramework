using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    /// <summary>
    /// 类似 Dota2 的伪随机分布系统。
    /// percentage 使用 0~100，比如 25 = 25%。
    ///
    /// 特点：
    /// 1. 长期平均触发率接近传入的 percentage。
    /// 2. 连续未触发时，下一次触发概率会逐渐提高。
    /// 3. 触发后，连续失败次数重置。
    ///
    /// 这个版本不再强引用 Unit / AbilityBase。
    /// 伪随机状态通过 ownerId + sourceId + type 区分。
    ///
    /// 推荐规则：
    /// 1. 单位自身效果：ownerId = unitRuntimeId, sourceId = 0, type = 效果类型
    /// 2. 技能效果：ownerId = unitRuntimeId, sourceId = abilityRuntimeId, type = 效果类型
    /// 3. 物品效果：ownerId = unitRuntimeId, sourceId = itemRuntimeId, type = 效果类型
    /// 4. 全局掉落：ownerId = 0, sourceId = 0, type = 掉落类型
    ///
    /// 注意：
    /// 1. ownerId / sourceId 必须是运行时实例 ID，不建议用配置 ID。
    /// 2. 如果 ID 会被对象池复用，复用前仍然需要 Clear。
    /// 3. 如果 ID 整局唯一不复用，可以只在战斗结束 / 关卡切换时 ClearAll。
    /// </summary>
    public static class PRDChanceSystem
    {
        private const double Epsilon = 0.000001d;

        private static readonly Dictionary<int, double> cCache = new();

        private static readonly Dictionary<PRDKey, PRDState> stateDict = new();

        private readonly struct PRDKey : IEquatable<PRDKey>
        {
            public readonly long ownerId;
            public readonly long sourceId;
            public readonly int type;

            public PRDKey(long ownerId, long sourceId, int type)
            {
                this.ownerId = ownerId;
                this.sourceId = sourceId;
                this.type = type;
            }

            public bool Equals(PRDKey other)
            {
                return ownerId == other.ownerId
                    && sourceId == other.sourceId
                    && type == other.type;
            }

            public override bool Equals(object obj)
            {
                return obj is PRDKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + ownerId.GetHashCode();
                    hash = hash * 31 + sourceId.GetHashCode();
                    hash = hash * 31 + type;
                    return hash;
                }
            }
        }

        private sealed class PRDState
        {
            public double targetPercentage;
            public double c;
            public int failCount;
        }

        /// <summary>
        /// 伪随机概率判定。
        /// percentage 使用 0~100。
        ///
        /// ownerId：一般传单位运行时 ID。
        /// sourceId：一般传技能 / 物品 / Buff 的运行时 ID；没有来源可传 0。
        /// type：同一个 owner + source 下，不同随机效果的区分类型。
        /// </summary>
        public static bool Chance(
            double percentage,
            long ownerId,
            long sourceId = 0L,
            int type = 0,
            RandomChannel channel = RandomChannel.Combat)
        {
            percentage = Math.Clamp(percentage, 0d, 100d);

            if (percentage <= 0d)
                return false;

            if (percentage >= 100d)
                return true;

            PRDKey key = new(ownerId, sourceId, type);
            PRDState state = GetState(key, percentage);

            double currentPercentage = GetCurrentPercentage(state);

            bool isTrigger = RVGlobals.RandomFloat(0f, 100f, channel) < currentPercentage;

            if (isTrigger)
                state.failCount = 0;
            else
                state.failCount++;

            return isTrigger;
        }

        /// <summary>
        /// 带额外概率加成的伪随机判定。
        /// bonusPercentage 由外部提前算好。
        /// </summary>
        public static bool Chance(
            double percentage,
            double bonusPercentage,
            long ownerId,
            long sourceId = 0L,
            int type = 0,
            RandomChannel channel = RandomChannel.Combat)
        {
            return Chance(percentage + bonusPercentage, ownerId, sourceId, type, channel);
        }

        /// <summary>
        /// 带角色属性加成的伪随机判定。
        ///
        /// 注意：
        /// 这里虽然传入 Unit target，但只用于读取属性。
        /// PRD 系统不会保存 Unit 引用。
        /// </summary>
        public static bool Chance(
            double percentage,
            string statChance,
            Unit target,
            long ownerId,
            long sourceId = 0L,
            int type = 0,
            RandomChannel channel = RandomChannel.Combat)
        {
            double statsChance = 0d;

            if (target != null && !string.IsNullOrEmpty(statChance))
                statsChance = target.statsC.GetStatsValue(statChance);

            return Chance(percentage + statsChance, ownerId, sourceId, type, channel);
        }

        /// <summary>
        /// 不叠加角色概率属性的伪随机判定。
        /// 保留这个名字，方便替换你原来的 ChanceWithoutStats。
        /// </summary>
        public static bool ChanceWithoutStats(
            double percentage,
            long ownerId,
            long sourceId = 0L,
            int type = 0,
            RandomChannel channel = RandomChannel.Global)
        {
            return Chance(percentage, ownerId, sourceId, type, channel);
        }

        /// <summary>
        /// 获取当前这一次的实际触发概率。
        /// 返回 0~100。
        /// 不会改变连续失败次数。
        /// </summary>
        public static double GetCurrentChance(
            double percentage,
            long ownerId,
            long sourceId = 0L,
            int type = 0)
        {
            percentage = Math.Clamp(percentage, 0d, 100d);

            if (percentage <= 0d)
                return 0d;

            if (percentage >= 100d)
                return 100d;

            PRDKey key = new(ownerId, sourceId, type);
            PRDState state = GetState(key, percentage);

            return GetCurrentPercentage(state);
        }

        /// <summary>
        /// 获取带额外概率加成后的当前实际触发概率。
        /// 返回 0~100。
        /// 不会改变连续失败次数。
        /// </summary>
        public static double GetCurrentChance(
            double percentage,
            double bonusPercentage,
            long ownerId,
            long sourceId = 0L,
            int type = 0)
        {
            return GetCurrentChance(percentage + bonusPercentage, ownerId, sourceId, type);
        }

        /// <summary>
        /// 获取带角色属性加成后的当前实际触发概率。
        /// 返回 0~100。
        /// 不会改变连续失败次数。
        ///
        /// 注意：
        /// 这里虽然传入 Unit target，但只用于读取属性。
        /// PRD 系统不会保存 Unit 引用。
        /// </summary>
        public static double GetCurrentChance(
            double percentage,
            string statChance,
            Unit target,
            long ownerId,
            long sourceId = 0L,
            int type = 0)
        {
            double statsChance = 0d;

            if (target != null && !string.IsNullOrEmpty(statChance))
                statsChance = target.statsC.GetStatsValue(statChance);

            return GetCurrentChance(percentage + statsChance, ownerId, sourceId, type);
        }

        /// <summary>
        /// 获取当前连续失败次数。
        /// </summary>
        public static int GetFailCount(
            long ownerId,
            long sourceId = 0L,
            int type = 0)
        {
            PRDKey key = new(ownerId, sourceId, type);

            if (stateDict.TryGetValue(key, out PRDState state))
                return state.failCount;

            return 0;
        }

        private static PRDState GetState(PRDKey key, double percentage)
        {
            if (!stateDict.TryGetValue(key, out PRDState state))
            {
                state = new PRDState();
                stateDict.Add(key, state);
            }

            if (Math.Abs(state.targetPercentage - percentage) > Epsilon)
            {
                state.targetPercentage = percentage;
                state.c = GetC(percentage);
            }

            return state;
        }

        private static double GetCurrentPercentage(PRDState state)
        {
            double chance01 = state.c * (state.failCount + 1);

            if (chance01 >= 1d)
                return 100d;

            return chance01 * 100d;
        }

        /// <summary>
        /// 根据目标平均概率 percentage，计算 PRD 常数 C。
        /// </summary>
        private static double GetC(double percentage)
        {
            int cacheKey = (int)Math.Round(percentage * 10000d);

            if (cCache.TryGetValue(cacheKey, out double cachedC))
                return cachedC;

            double targetP = percentage / 100d;

            double low = 0d;
            double high = targetP;

            for (int i = 0; i < 64; i++)
            {
                double mid = (low + high) * 0.5d;
                double actualP = GetActualProbability(mid);

                if (actualP > targetP)
                    high = mid;
                else
                    low = mid;
            }

            double c = (low + high) * 0.5d;
            cCache[cacheKey] = c;
            return c;
        }

        /// <summary>
        /// 给定 C，计算长期平均触发率。
        /// 返回 0~1。
        /// </summary>
        private static double GetActualProbability(double c)
        {
            if (c <= 0d)
                return 0d;

            if (c >= 1d)
                return 1d;

            double failProbability = 1d;
            double expectedAttempts = 0d;

            for (int n = 1; n < 100000; n++)
            {
                double currentChance = c * n;

                if (currentChance > 1d)
                    currentChance = 1d;

                double triggerProbability = failProbability * currentChance;

                expectedAttempts += n * triggerProbability;

                failProbability *= 1d - currentChance;

                if (failProbability <= 0.000000000001d)
                    break;
            }

            if (expectedAttempts <= 0d)
                return 0d;

            return 1d / expectedAttempts;
        }

        /// <summary>
        /// 清除某个 ownerId + sourceId + type 的伪随机状态。
        /// 如果你的运行时 ID 不复用，平时一般不需要频繁调用。
        /// 如果 ID 会被对象池复用，复用前必须调用。
        /// </summary>
        public static void Clear(
            long ownerId,
            long sourceId = 0L,
            int type = 0)
        {
            stateDict.Remove(new PRDKey(ownerId, sourceId, type));
        }

        /// <summary>
        /// 清除某个 ownerId 下所有伪随机状态。
        /// 适合单位释放 / 单位死亡并且 ID 可能复用时调用。
        /// </summary>
        public static void ClearByOwner(long ownerId)
        {
            if (stateDict.Count == 0)
                return;

            List<PRDKey> removeList = new();

            foreach (PRDKey key in stateDict.Keys)
            {
                if (key.ownerId == ownerId)
                    removeList.Add(key);
            }

            for (int i = 0; i < removeList.Count; i++)
                stateDict.Remove(removeList[i]);
        }

        /// <summary>
        /// 清除某个 sourceId 下所有伪随机状态。
        /// 适合技能 / 物品 / Buff 释放并且 ID 可能复用时调用。
        /// </summary>
        public static void ClearBySource(long sourceId)
        {
            if (stateDict.Count == 0)
                return;

            List<PRDKey> removeList = new();

            foreach (PRDKey key in stateDict.Keys)
            {
                if (key.sourceId == sourceId)
                    removeList.Add(key);
            }

            for (int i = 0; i < removeList.Count; i++)
                stateDict.Remove(removeList[i]);
        }

        /// <summary>
        /// 清除某个 ownerId + sourceId 下所有 type 的伪随机状态。
        /// 适合某个单位身上的某个技能 / 物品 / Buff 被移除时调用。
        /// </summary>
        public static void ClearByOwnerSource(
            long ownerId,
            long sourceId)
        {
            if (stateDict.Count == 0)
                return;

            List<PRDKey> removeList = new();

            foreach (PRDKey key in stateDict.Keys)
            {
                if (key.ownerId == ownerId && key.sourceId == sourceId)
                    removeList.Add(key);
            }

            for (int i = 0; i < removeList.Count; i++)
                stateDict.Remove(removeList[i]);
        }

        /// <summary>
        /// 清除所有伪随机状态。
        /// 适合切换关卡、重新开始游戏、战斗完全结束时调用。
        /// </summary>
        public static void ClearAll()
        {
            stateDict.Clear();
        }

        /// <summary>
        /// 清除 C 值缓存。
        /// 一般不需要调用。
        /// </summary>
        public static void ClearCache()
        {
            cCache.Clear();
        }
    }
}