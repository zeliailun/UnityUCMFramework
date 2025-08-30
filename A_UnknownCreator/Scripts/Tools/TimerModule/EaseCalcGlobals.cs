namespace UnknownCreator.Modules
{
    public static class EaseCalcGlobals
    {
        public static float Calc(EaseTypes type, float start, float end, float value)
        {
            return type switch
            {
                EaseTypes.Linear => Linear(start, end, value),
                EaseTypes.BounceIn => BounceIn(start, end, value),
                EaseTypes.BounceOut => BounceOut(start, end, value),
                EaseTypes.BounceInOut => BounceInOut(start, end, value),
                _ => Linear(start, end, value)// 默认线性插值
            };
        }

        private static float Linear(float start, float end, float value)
        {
            return start + (end - start) * value;
        }

        private static float BounceIn(float start, float end, float value)
        {
            end -= start;
            float d = 1f;
            return end - BounceOut(0, end, d - value) + start;
        }

        private static float BounceOut(float start, float end, float value)
        {
            value /= 1f;
            end -= start;
            if (value < (1 / 2.75f))
            {
                return end * (7.5625f * value * value) + start;
            }
            else if (value < (2 / 2.75f))
            {
                value -= (1.5f / 2.75f);
                return end * (7.5625f * (value) * value + .75f) + start;
            }
            else if (value < (2.5 / 2.75))
            {
                value -= (2.25f / 2.75f);
                return end * (7.5625f * (value) * value + .9375f) + start;
            }
            else
            {
                value -= (2.625f / 2.75f);
                return end * (7.5625f * (value) * value + .984375f) + start;
            }
        }

        private static float BounceInOut(float start, float end, float value)
        {
            end -= start;
            float d = 1f;
            if (value < d * 0.5f) return BounceOut(0, end, value * 2) * 0.5f + start;
            else return BounceOut(0, end, value * 2 - d) * 0.5f + end * 0.5f + start;
        }
    }
}