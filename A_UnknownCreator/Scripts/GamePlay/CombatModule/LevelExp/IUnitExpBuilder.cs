using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public interface IUnitExpBuilder
    {
        List<double> ExpBuilder(int maxLv, Unit unit = null);
    }
}