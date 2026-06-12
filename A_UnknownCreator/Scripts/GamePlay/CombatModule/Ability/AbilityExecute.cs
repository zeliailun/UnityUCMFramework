using System;
using Animancer;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public abstract partial class AbilityBase
    {
        internal void ExecuteAbilityInterrupt(bool isPointOrBackswing)
        {
            GameEvtBus.Send<EvtAbilityInterrupt>(new(this, owner, isPointOrBackswing));
            if (isPointOrBackswing)
            {
                timerCastPoint.DestroySelf();
                timerCastPoint = null;
                owner.abilityC.SetCastPoint(false);
            }
            else
            {
                timerCastBackswing.DestroySelf();
                timerCastBackswing = null;
                owner.abilityC.SetCastBackswing(false);
                GameEvtBus.Send<EvtAbilityFullyCast>(new(this,owner));
            }
            if (isCastAnimPlaying)
                ap.FadeOutLayer();
            selectedTarget = null;
            castAnimState = null;
            owner.abilityC.SetCastAbility(null);
            ApplyState(-1);
            //
        }

        internal void ExecuteAbilityOnImmediate()
        {
            if (HasBehavior(AbBehavior.Immediate))
                Executing();
        }

        internal void ExecuteAbilityOnPosition(Vector3 pos)
        {
            if (HasBehavior(AbBehavior.Point))
            {
                selectedPos = pos;
                Executing();
            }
        }

        internal void ExecuteAbilityOnTarget(Unit target)
        {
            if (HasBehavior(AbBehavior.Target))
            {
                selectedTarget = target;
                selectedPos = selectedTarget.entT.position;
                Executing();
            }
        }

        internal void ExecuteAbilityPressed()
        {
            if (HasOnlyBehavior(AbBehavior.None)) return;

            InevitableCastPressed();

            modeCache = GetTriggerMode();
            if (modeCache == AbTriggerMode.Pressed)
                Executing();
        }

        internal void ExecuteAbilityReleased()
        {
            if (HasOnlyBehavior(AbBehavior.None)) return;

            InevitableCastReleased();

            if (modeCache == AbTriggerMode.Released)
                Executing();
        }

        private void Executing()
        {

            if (!CastFilter()) return;

            GameEvtBus.Send<EvtAbilityStart>(new(this, owner));

            if (IsForceCastDir())
            {
                var newPos = selectedPos;
                newPos.y = owner.entT.position.y;
                owner.entT.forward = UnityGlobals.Direction(newPos, owner.entT.position);
            }

            var castPointDuration = GetCastPoint(level);
            newCastAnimName = GetCastAnim();
            if (owner.animC.isAnimancerReady && !string.IsNullOrWhiteSpace(newCastAnimName))
            {
                if (!newCastAnimName.Equals(castAnimName))
                {
                    ap.SetAnimAsset(newCastAnimName);
                    castAnimName = newCastAnimName;
                }

                AnimPlayerInfo info = new();
                info.anim = owner.animC.anim;
                info.startFade = GetAnimStartFadeDuration();
                info.endFade = GetAnimEndFadeDuration();
                info.startLayer = GetCastAnimLayers();
                info.endWeight = GetCastAnimEndWeight();
                info.mask = GetAvatarMask();
                info.fadeMode = FadeMode.FromStart;
                info.fadeGroup = Easing.Function.Linear;
                info.sp = 1;
                castAnimState = ap.Play(info);
                castAnimState.Speed = (float)(GetForceAnimSp() ? GetAnimSp() : Math.Clamp(GetAnimTriggerTime() / castAnimState.Length / (castPointDuration / castAnimState.Length), 0, 99999));
            }



            if (HasBehavior(AbBehavior.Immediate) || castPointDuration <= 0)
            {

                TriggerAbility();
                selectedTarget = null;
                GameEvtBus.Send<EvtAbilityFullyCast>(new(this, owner));
            }
            else
            {
                ApplyState(1);
                owner.abilityC.SetCastAbility(this);
                owner.abilityC.SetCastPoint(true);
                timerCastPoint.DestroySelf();
                timerCastPoint = Mgr.Timer.CycleCount(1, (float)castPointDuration, false, null, castPointAct);
            }
        }

        private void EndCastPoint(TimerCountCycle cycle)
        {
            owner.abilityC.SetCastPoint(false);
            TriggerAbility();
            if (HasFlags(AbFlags.IgnoreBackswing) || !castAnimState.IsValid() || castAnimState.RemainingDuration <= 0)
            {
                EndCastBackswing(null);
            }
            else
            {
                owner.abilityC.SetCastBackswing(true);
                timerCastBackswing.DestroySelf();
                timerCastBackswing = Mgr.Timer.CycleCount(1, castAnimState.RemainingDuration, false, null, castBackswingAct);
            }
        }

        private void EndCastBackswing(TimerCountCycle cycle)
        {
            if (isCastAnimPlaying) ap.FadeOutLayer();
            owner.abilityC.SetCastBackswing(false);
            owner.abilityC.SetCastAbility(null);
            selectedTarget = null;
            castAnimState = null;
            ApplyState(-1);
            GameEvtBus.Send<EvtAbilityFullyCast>(new(this, owner));
        }

        private void TriggerAbility()
        {
            StartCooldown(GetCooldown(level));
            OnCastTrigger();
            GameEvtBus.Send<EvtAbilityExecuted>(new(this, owner));
        }


        //施法过滤

        private bool CastFilter()
        {
            bool isGamePaused = !IsGamePauseCast() && CustomTime.IsPause;
            bool isCastPoint = owner.abilityC.isCastPoint;
            bool isCastBackswing = owner.abilityC.isCastBackswing && !HasFlags(AbFlags.InterruptOtherCastBackswing);
            bool isCustomFilter = !GetCustomCastFilter();

            if (isGamePaused || isCastPoint || isCastBackswing || isCustomFilter)
            {
                if (isCustomFilter && IsEnableCharge())
                {
                    int id = GetCustomCastFilterID();
                    if (id == -1)
                        id = AbilityGlobals.InvalidCast;
                    GameEvtBus.Send<EvtAbilityCastError>(new(this, owner, id));
                }

                return false;
            }

            if (!isLevelReady)
            {
                GameEvtBus.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidLevel));
                return false;
            }

            if (IsEnableCharge())
            {
                if (currentCharge < 1)
                {
                    GameEvtBus.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidCharge));
                    return false;
                }
            }
            else if (!isCooldownReady)
            {
                GameEvtBus.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidCooldown));
                return false;
            }



            if (!isIgnoreCastRange)
            {
                if (Mgr.Cntlr.IsControllerTarget(owner.ent) &&
                    Physics.Raycast(Mgr.Camera.mainCam.ScreenPointToRay(GetInputValue()), out var hit, MathGlobals.PInfinity, ~(1 << 2)))
                    selectedPos = hit.point;

                if (!IsEnoughCastRange(UnityGlobals.DistanceH(owner.entP, selectedPos)))
                {
                    GameEvtBus.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidCastRange));
                    return false;
                }
            }

            if (!isStunnedCast)
            {
                GameEvtBus.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidStunned));
                return false;
            }

            if (!isSilencedCast)
            {
                GameEvtBus.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidSilenced));
                return false;
            }

            if (HasFlags(AbFlags.InterruptOtherCastBackswing) && owner.abilityC.isCastBackswing)
                owner.abilityC.InterruptAbility(false);

            return (!HasBehavior(AbBehavior.Target) || HasBehavior(AbBehavior.Immediate) || HasBehavior(AbBehavior.NotTarget) || TargetFilter()) && OnCastStart();
        }

        private bool TargetFilter()
        {
            if (HasOnlyTargetTeam(AbTargetTeam.None)) return false;

            if (Mgr.Cntlr.IsControllerTarget(owner.ent) &&
                Physics.Raycast(Mgr.Camera.mainCam.ScreenPointToRay(GetInputValue()), out var hit, MathGlobals.PInfinity, 1 << Mgr.Unit.hitBoxLayer))
                selectedTarget = hit.collider.gameObject.GetUnitByHitBox();

            if (selectedTarget is null)
            {
                if (HasBehavior(AbBehavior.Point)) return true;
                GameEvtBus.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.NotTarget));
                return false;
            }

            if (!selectedTarget.isAlive && !HasFlags(AbFlags.CanDeathTarget))
            {
                if (HasBehavior(AbBehavior.Point)) return true;
                GameEvtBus.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.DeadTarget));
                return false;
            }

            if (HasTargetTeam(AbTargetTeam.Self) && selectedTarget == owner ||
                HasTargetTeam(AbTargetTeam.Enemy) && selectedTarget.unitTeam != owner.unitTeam ||
                HasTargetTeam(AbTargetTeam.Friendly) && owner.unitTeam == selectedTarget.unitTeam)
                return true;

            GameEvtBus.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidTeam));
            return false;
        }
    }
}
