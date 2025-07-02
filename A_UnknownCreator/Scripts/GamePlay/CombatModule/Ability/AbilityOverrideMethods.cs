using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnknownCreator.Modules
{
    public abstract partial class AbilityBase
    {
        /// <summary>
        /// 施法前的最后一次过滤
        /// </summary>
        /// <returns></returns>
        public virtual bool GetCustomCastFilter() => true;

        public virtual int GetCustomCastFilterID() => -1;

        /// <summary>
        /// 初始化完成时调用,还未添加到列表中,用来做一些变量初始化【不建议添加逻辑内容】
        /// </summary>
        public virtual void OnInitialized() { }

        /// <summary>
        /// 创建完成时调用
        /// </summary>
        public virtual void OnCreated() { }

        /// <summary>
        /// 当更新时
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// 进入对象池前调用
        /// </summary>
        protected virtual void OnRelease() { }

        /// <summary>
        /// 施法开始前调用，在自定义过滤器之后
        /// </summary>
        /// <returns></returns>
        public virtual bool OnCastStart() => true;

        /// <summary>
        /// 当施法被中断时调用
        /// </summary>
        public virtual void OnCastInterrupt() { }

        /// <summary>
        /// 施法触发
        /// </summary>
        public virtual void OnCastTrigger() { }


        public virtual void InevitableCastPressed() { }

        public virtual void InevitableCastReleased() { }

        public virtual void OnProjectileSpawn(Projectile proj) { }

        public virtual void OnProjectileHit(Projectile proj, Unit target, GameObject obj, RaycastHit hit) { }

        public virtual void OnProjectileMotion(Projectile projD) { }

        public virtual void OnProjectilePause(Projectile proj) { }

        public virtual void OnProjectileDestroy(Projectile proj) { }


        protected virtual void OnOwnerDead() { }

        protected virtual void OnOwnerRespawn() { }


        public virtual double GetCastPoint(int lv) => GetStatValue(AbilityGlobals.StatCastPoint, lv);

        public virtual double GetCooldown(int lv) => GetStatValue(AbilityGlobals.StatCooldown, lv);

        public virtual double GetCastRange(int lv) => GetStatValue(AbilityGlobals.StatCastRange, lv);

        public virtual double GetCastRangeBuffer(int lv) => GetStatValue(AbilityGlobals.StatCastRangeBuffer, lv);

        public virtual int GetCharge(int lv) => (int)GetStatValue(AbilityGlobals.StatCharge, lv);


        protected virtual Vector2 GetInputValue()
        => Mouse.current.position.ReadValue();

        public virtual Texture2D GetIcon()
        {
            if (icon != null)
                return icon;

            if (!string.IsNullOrWhiteSpace(abilityCfg.icon))
                icon = UnityGlobals.LoadSync<Texture2D>(abilityCfg.icon);

            return icon;
        }

        public virtual int GetAbilityType() => abilityCfg.abilityType;

        public virtual AbBehavior GetBehaviorType() => abilityCfg.abilityCastType;

        public virtual AbTargetTeam GetTargetTeamType() => abilityCfg.abilityTargetTeamType;

        public virtual AbFlags GetFlagsType() => abilityCfg.abilityFlags;

        public virtual AbTriggerMode GetTriggerMode() => abilityCfg.useMode;

        public virtual int[] GetDisableState() => abilityCfg.stateID;

        public virtual bool IsForceCastDir() => abilityCfg.isForceCastDir;

        public virtual bool IsRefreshable() => abilityCfg.isRefreshable;

        public virtual string GetCurrentPassiveName() => abilityCfg.defaultPassiveName;

        public virtual string GetCastAnim() => castAnimName;

        public virtual AvatarMask GetAvatarMask() => abilityCfg.mask;

        public virtual int GetCastAnimLayers() => abilityCfg.animLayers;

        public virtual int GetCastAnimEndWeight() => abilityCfg.animEndWeight;

        public virtual bool GetForceAnimSp() => abilityCfg.isForceAnimSp;

        public virtual float GetAnimSp() => abilityCfg.animSp;

        public virtual float GetAnimTriggerTime() => abilityCfg.animTriggerTime;

        public virtual float GetAnimStartFadeDuration() => abilityCfg.animStartFadeDuration;

        public virtual float GetAnimEndFadeDuration() => abilityCfg.animEndFadeDuration;

        public virtual LayerMask GetSelectedPosLayerMask() => ~(1 << 2);

        public virtual bool IsGamePauseCast() => abilityCfg.isGamePauseCast;
        public virtual bool IsEnableCharge() => abilityCfg.isEnableCharge;
    }
}