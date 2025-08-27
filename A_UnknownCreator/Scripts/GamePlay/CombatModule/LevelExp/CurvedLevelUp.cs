using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    [Serializable]
    public class CurvedLevelUp : IUnitExpBuilder
    {
        public double baseExperience;

        public double curveFactor;

        public double experienceIncrement;

        public List<double> ExpBuilder(int maxLv, Unit unit = null)
        {
            List<double> result = new();
            for (int k = 0; k < maxLv; k++)
                result.Add((baseExperience + Math.Round(curveFactor + Math.Log(k + 1)) * experienceIncrement));
            return result;
        }
    }

}

