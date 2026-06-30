
namespace UnknownCreator.Modules
{
    public enum CalcType
    {
        /// <summary>
        /// 常量覆盖。
        /// 直接把属性最终计算值设为指定值。
        /// 存在 Constant 时，不再吃其他加成。
        /// 多个 Constant 同时存在时，后遍历到的 Constant 覆盖前面的。
        /// </summary>
        Constant = 0,

        /// <summary>
        /// 线性加法。
        /// 直接加固定数值。
        /// baseValue 为 0 时也能生效。
        /// 例：100 + 20 = 120。
        /// </summary>
        LinearAdd = 1,

        /// <summary>
        /// 百分比线性加法。
        /// 多个百分比先相加，再统一乘到 value 上。
        /// 例：+10% +20% = +30%。
        /// value 为 0 时，乘完仍然是 0。
        /// </summary>
        PercLinearAdd = 2,

        /// <summary>
        /// 百分比乘算。
        /// 每条百分比独立相乘。
        /// 例：+10% 和 +20% = *1.1 *1.2。
        /// value 为 0 时，乘完仍然是 0。
        /// </summary>
        PercMul = 3,

        /// <summary>
        /// 百分比双曲递减，乘法版。
        /// 使用 1 - (1 - p)^n 计算递减收益。
        /// 有效百分比趋近 100%。
        /// 最后作为倍率乘到 value 上。
        /// 适合有基础值的属性。
        /// </summary>
        PercHyperbolic = 4,

        /// <summary>
        /// 百分比软上限，乘法版。
        /// 多个百分比先相加，再进入无限递减收益公式。
        /// 可以无限叠加，但越叠收益越低。
        /// 最后作为倍率乘到 value 上。
        /// 适合攻击力%、移速%、范围%、射速%。
        /// </summary>
        PercSoftCap = 5,

        /// <summary>
        /// 数值软上限，加法版。
        /// 多个数值先相加，再进入无限递减收益公式。
        /// 可以无限叠加，但越叠收益越低。
        /// 直接加到 value 上。
        /// baseValue 为 0 时也能生效。
        /// </summary>
        SoftCapAdd = 6,

        /// <summary>
        /// 双曲递减，加法版。
        /// 使用 1 - (1 - p)^n 计算递减收益。
        /// 有效值趋近 100。
        /// 最后直接加到 value 上。
        /// baseValue 为 0 时也能生效。
        /// 适合暴击率、闪避率、减伤率、冷却缩减等默认 0 起步，
        /// 并且希望最终接近 100 的属性。
        /// </summary>
        HyperbolicAdd = 7
    }
}