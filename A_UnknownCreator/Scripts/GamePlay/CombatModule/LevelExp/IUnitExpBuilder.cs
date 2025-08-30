using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public interface IUnitExpBuilder
    {
        List<double> ExpBuilder(int maxLv, Unit unit = null);
    }
}