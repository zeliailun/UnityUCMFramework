using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public interface IUnitMgr : IDearMgr
    {
        Func<(Unit, double), bool> FilterExpAdd { set; get; }

        int unitLayer { get; }

        int hitBoxLayer { get; }

        int unitStateCount { get; }

        int unitTeamCount { get; }

        int unitTypeCount { get; }

        bool isUseGlobalLevelExp { get; }

        int unitMaxLevel { get; }

        IUnitExpBuilder expBuilder { get; }

        List<double> unitExpList { get; }

        void UpdateMaxLevelAndFormula(IUnitExpBuilder expBuilder, int value);
        void AddUnitRoot(int selfID, Unit unit);
        void RemoveUnitRoot(int selfID);
        Unit GetUnitRoot(int selfID);
    }
}