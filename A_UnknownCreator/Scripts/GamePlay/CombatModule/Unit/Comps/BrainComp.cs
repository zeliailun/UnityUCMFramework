namespace UnknownCreator.Modules
{
    public class BrainComp : StateComp
    {
        public IHBSMController hbsmPlayer { private set; get; }

        public IHBSMController hbsmAI { private set; get; }

        public IHBSMController hbsmCurrent { private set; get; }

        public BrainType brainType { private set; get; }

        public bool hasBrain
        => hbsmCurrent != null;

        private Unit self;

        public override void InitComp()
        {
            self = kv.GetValue<Unit>();
            brainType = BrainType.None;
            hbsmPlayer ??= new HBSMController();
            hbsmPlayer.kv.AddValue<Unit>(self);
            hbsmAI ??= new HBSMController();
            hbsmAI.kv.AddValue<Unit>(self);
        }

        public override void EnableComp()
        {
            hbsmCurrent?.EnableAllHBSM();
        }

        public override void DisableComp()
        {
            hbsmCurrent?.DisableAllHBSM();
        }

        public override void UpdateComp()
        {
            hbsmCurrent?.UpdateAllHBSM();
        }

        public override void FixedUpdateComp()
        {
            hbsmCurrent?.FixedUpdateAllHBSM();
        }

        public override void LateUpdateComp()
        {
            hbsmCurrent?.LateUpdateAllHBSM();
        }

        public override void ReleaseComp()
        {
            hbsmAI.ReleaseAllHBSM();
            hbsmPlayer.ReleaseAllHBSM();
            hbsmCurrent = null;
            self = null;
        }

        public void SwitchToPlayer()
        {
            if (!self.enable || brainType == BrainType.Player) return;
            SetCurrentBrainHBSM(true);
            Mgr.Camera.ChangeTarget(self.ent);
            Mgr.Cntlr.ChangeTarget(self.ent);
        }

        public void SwitchToAI()
        {
            if (!self.enable || brainType == BrainType.AI) return;
            if (Mgr.Cntlr.IsControllerTarget(self.ent))
            {
                Mgr.Camera.ChangeTarget(null);
                Mgr.Cntlr.ChangeTarget(null);
            }
            SetCurrentBrainHBSM(false);
        }

        private void SetCurrentBrainHBSM(bool isPlayer)
        {
            if (!self.enable) return;

            hbsmCurrent = null;
            if (isPlayer)
            {
                hbsmAI.DisableAllHBSM();
                hbsmPlayer.EnableAllHBSM();
                hbsmCurrent = hbsmPlayer;
                brainType = BrainType.Player;
            }
            else
            {
                hbsmPlayer.DisableAllHBSM();
                hbsmAI.EnableAllHBSM();
                hbsmCurrent = hbsmAI;
                brainType = BrainType.AI;
            }
        }
    }

    public enum BrainType
    {
        None,
        AI,
        Player
    }
}