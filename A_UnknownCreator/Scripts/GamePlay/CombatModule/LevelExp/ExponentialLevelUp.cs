using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    [Serializable]
    public class ExponentialLevelUp : IUnitExpBuilder
    {
        public double baseExperience;

        public double exponent;

        public double experienceIncrement;

        public List<double> ExpBuilder(int maxLv, Unit unit = null)
        {
            List<double> result = new();
            for (int k = 0; k < maxLv; k++)
                result.Add((baseExperience + Math.Round(Math.Pow(k + 1, exponent)) * experienceIncrement));
            return result;
        }
    }

}
