namespace UnknownCreator.Modules
{
    public static partial class CombatEvtGlobals
    {
        #region 技能

        /// <summary>
        /// 【当技能等级改变时】
        /// EvtAbilityLevelChanged
        /// AbilityBase ability：能力
        /// Unit owner：单位
        /// int oldLv：旧等级
        /// int newLv：新等级
        /// </summary>
        public const string OnAbilityLevelChanged = nameof(OnAbilityLevelChanged);

        /// <summary>
        /// 【技能冷却开始计算时】
        /// EvtAbilityCooldownStart
        /// AbilityBase ability：能力
        /// Unit owner：单位
        /// double oldCooldown：旧冷却
        /// double newCooldown：新冷却
        /// </summary>
        public const string OnAbilityCooldownStart = nameof(OnAbilityCooldownStart);

        /// <summary>
        /// 【技能冷却计算时，每帧触发】
        /// AbilityBase
        /// </summary>
        public const string OnAbilityCooldownCalc = nameof(OnAbilityCooldownCalc);

        /// <summary>
        /// 【添加技能后】
        ///  AbilityBase
        /// </summary>
        public const string OnAbilityAdded = nameof(OnAbilityAdded);

        /// <summary>
        /// 【移除技能前】
        ///   AbilityBase
        /// </summary>
        public const string OnRemoveAbility = nameof(OnRemoveAbility);

        /// <summary>
        /// 【技能释放前触发，返回：false停止,true执行】
        ///  AbilityBase
        /// </summary>
        public const string OnAbilityStart = nameof(OnAbilityStart);

        /// <summary>
        /// 【技能触发后执行】
        ///  AbilityBase
        /// </summary>
        public const string OnAbilityExecuted = nameof(OnAbilityExecuted);

        /// <summary>
        /// 【完全执行时触发，通常在前摇或后摇结束后】
        /// AbilityBase
        /// </summary>
        public const string OnAbilityFullyCast = nameof(OnAbilityFullyCast);

        /// <summary>
        /// 【中断前摇时触发】
        /// EvtAbilityInterrupt
        /// AbilityBase ability：能力
        /// Unit owner：单位
        /// bool isPointOrBackswing：是前摇还是后摇阶段
        /// </summary>
        public const string OnAbilityCastPointInterrupt = nameof(OnAbilityCastPointInterrupt);

        /// <summary>
        /// 技能施法过滤
        /// EvtAbilityCastError
        ///  AbilityBase,Unit,AbSpellCastError
        /// </summary>
        public const string OnAbilityInvalidSpellCast = nameof(OnAbilityInvalidSpellCast);


        /// <summary>
        /// 技能充能改变时
        /// EvtAbilityChargeChanged
        /// AbilityBase,Unit,Value
        /// </summary>
        public const string OnAbilityChargeChanged = nameof(OnAbilityChargeChanged);

        #endregion



        #region Buff

        /// <summary>
        /// BuffBase
        /// </summary>
        public const string OnBuffAdded = nameof(OnBuffAdded);

        /// <summary>
        /// Buff运动更新中断时
        /// BuffBase:中断的BUFF
        /// </summary>
        internal const string MotionInterrupted = nameof(MotionInterrupted);

        #endregion



        #region 投射物

        /// <summary>
        /// Projectile
        /// </summary>
        public const string OnProjectileSpawned = nameof(OnProjectileSpawned);

        /// <summary>
        ///  EvtProjectileHitAfter
        /// Projectile Unit GameObject Vector3 
        /// </summary>
        public const string OnProjectileHitAfter = nameof(OnProjectileHitAfter);

        /// <summary>
        ///  Projectile
        /// </summary>
        public const string OnProjectileMotion = nameof(OnProjectileMotion);

        /// <summary>
        /// Projectile
        /// </summary>
        public const string OnProjectileDestroy = nameof(OnProjectileDestroy);

        #endregion



        #region 状态

        /// <summary>
        /// 单位状态更新后
        /// Unit typeID isState
        /// </summary>
        public const string OnStateUpdated = nameof(OnStateUpdated);

        #endregion



        #region 天赋

        /// <summary>
        /// 添加天赋后
        /// </summary>
        public const string OnTalentAdded = nameof(OnTalentAdded);

        /// <summary>
        /// 移除天赋后
        /// </summary>
        public const string OnTalentRemoved = nameof(OnTalentRemoved);

        #endregion



        #region 单位

        /// <summary>
        /// 团队更换后
        /// EvtUnitTeamChanged
        /// </summary>
        public const string OnUnitTeamChanged = nameof(OnUnitTeamChanged);


        /// <summary>
        /// 模型更换(需要单位id)
        /// return：string：模型名称
        /// </summary>
        public const string OnGetModelName = nameof(OnGetModelName);


        /// <summary>
        /// 模型已更换
        /// EvtUnitModelChanged
        /// </summary>
        public const string OnUnitModelChanged = nameof(OnUnitModelChanged);

        /// <summary>
        /// 升级后
        /// Unit:升级的单位
        /// </summary>
        public const string OnUnitUpgraded = nameof(OnUnitUpgraded);

        /// <summary>
        /// 添加经验后
        /// </summary>
        public const string OnUnitExpAdded = nameof(OnUnitExpAdded);

        #endregion
    }
}