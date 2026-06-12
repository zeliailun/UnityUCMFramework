
namespace UnknownCreator.Modules
{

    public enum CalcType
    {
        Constant,          // 直接覆盖最终值
        LinearAdd,         // 线性加法       +10
        PercLinearAdd,     // 线性百分比     +10%

        PercMul,           // 乘算           x1.1 x1.1
        PercHyperbolic,    // 双曲递减       1-(1-p)^n      递减叠加
        PercSoftCap        // SoftCap 软上限                衰减叠加
    }
}