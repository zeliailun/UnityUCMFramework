using System.Collections.Generic;
using Unity.Mathematics;

namespace UnknownCreator.Modules
{
    public class ExponentialLevelUp : IUnitExpBuilder
    {
        public double baseExperience;

        public double exponent;

        public double experienceIncrement;

        public List<double> ExpBuilder(int maxLv, Unit unit = null)
        {
            List<double> result = new();
            for (int k = 0; k < maxLv; k++)
                result.Add((baseExperience + math.round(math.pow(k + 1, exponent)) * experienceIncrement));
            return result;
        }
    }

}
