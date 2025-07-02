using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public class LinearLevelUp : IUnitExpBuilder
    {
        public double baseExperience;

        public double levelIncrease;

        public List<double> ExpBuilder(int maxLv, Unit unit = null)
        {
            List<double> result = new();
            for (int k = 0; k < maxLv; k++)
                result.Add(baseExperience + ((k+1) * levelIncrease));
            return result;
        }
    }

}