using UnityEngine;
using Animancer;
using System;

namespace UnknownCreator.Modules
{
    public abstract partial class AbilityBase
    {
        internal void ExecuteAbilityInterrupt(bool isPointOrBackswing)
        {
            Mgr.Event.Send<EvtAbilityInterrupt>(new(this, owner, isPointOrBackswing), CombatEvtGlobals.OnAbilityCastPointInterrupt);
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
            }
            if (isCastAnimPlaying)
                ap.FadeOutLayer();
            selectedTarget = null;
            castAnimState = null;
            owner.abilityC.SetCastAbility(null);
            ApplyState(-1);
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

            if (modeCache == AbTriggerMode.Released)
                Executing();

            InevitableCastReleased();
        }

        private void Executing()
        {

            if (!CastFilter()) return;

            owner.abilityC.InterruptAbility(false);

            Mgr.Event.Send<AbilityBase>(this, CombatEvtGlobals.OnAbilityStart);

            if (isFaceTargetPoint)
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
                    ap.SetPlayAnim(newCastAnimName);
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
                Mgr.Event.Send(this, CombatEvtGlobals.OnAbilityFullyCast);
                if (isCastAnimPlaying)
                    ap.FadeOutLayer();
                selectedTarget = null;
                castAnimState = null;
                owner.abilityC.SetCastAbility(null);
                ApplyState(-1);
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
            Mgr.Event.Send(this, CombatEvtGlobals.OnAbilityFullyCast);
            owner.abilityC.SetCastBackswing(false);
            owner.abilityC.SetCastAbility(null);
            selectedTarget = null;
            castAnimState = null;
            ApplyState(-1);

        }

        private void TriggerAbility()
        {
            StartCooldown(GetCooldown(level));
            OnCastTrigger();
            Mgr.Event.Send<AbilityBase>(this, CombatEvtGlobals.OnAbilityExecuted);
        }

        private bool CastFilter()
        {
            bool isGamePaused = !IsGamePauseCast() && CustomTime.IsPause;
            bool isCasting = owner.abilityC.isCastPoint;
            bool isInvalid = !GetCustomCastFilter();

            if (isGamePaused || isCasting || isInvalid)
            {
                if (isInvalid)
                {
                    int id = GetCustomCastFilterID();
                    if (id == -1)
                        id = AbilityGlobals.InvalidCast;

                    Mgr.Event.Send(new EvtAbilityCastError(this, owner, id), CombatEvtGlobals.OnAbilityInvalidSpellCast);
                }

                return false;
            }



            if (!isLevelReady)
            {
                Mgr.Event.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidLevel), CombatEvtGlobals.OnAbilityInvalidSpellCast);
                return false;
            }

            if (IsEnableCharge())
            {
                if (currentCharge < 1)
                {
                    Mgr.Event.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidCharge), CombatEvtGlobals.OnAbilityInvalidSpellCast);
                    return false;
                }
            }
            else if (!isCooldownReady)
            {
                Mgr.Event.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidCooldown), CombatEvtGlobals.OnAbilityInvalidSpellCast);
                return false;
            }



            if (!isIgnoreCastRange)
            {
                if (Mgr.Cntlr.IsControllerTarget(owner.ent) &&
                    Physics.Raycast(Mgr.Camera.mainCam.ScreenPointToRay(GetInputValue()), out var hit, MathUtils.PInfinity, ~(1 << 2)))
                    selectedPos = hit.point;

                if (!IsEnoughCastRange(UnityGlobals.DistanceH(owner.entP, selectedPos)))
                {
                    Mgr.Event.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidCastRange), CombatEvtGlobals.OnAbilityInvalidSpellCast);
                    return false;
                }
            }

            if (!isStunnedCast)
            {
                Mgr.Event.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidStunned), CombatEvtGlobals.OnAbilityInvalidSpellCast);
                return false;
            }

            if (!isSilencedCast)
            {
                Mgr.Event.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidSilenced), CombatEvtGlobals.OnAbilityInvalidSpellCast);
                return false;
            }

            return (!HasBehavior(AbBehavior.Target) || HasBehavior(AbBehavior.Immediate) || HasBehavior(AbBehavior.NotTarget) || TargetFilter()) && OnCastStart();
        }

        private bool TargetFilter()
        {
            if (HasOnlyTargetTeam(AbTargetTeam.None)) return false;

            if (Mgr.Cntlr.IsControllerTarget(owner.ent) &&
                Physics.Raycast(Mgr.Camera.mainCam.ScreenPointToRay(GetInputValue()), out var hit, MathUtils.PInfinity, 1 << Mgr.Unit.hitBoxLayer))
                selectedTarget = hit.collider.gameObject.GetUnitByHitBox();

            if (selectedTarget is null)
            {
                if (HasBehavior(AbBehavior.Point)) return true;
                Mgr.Event.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.NotTarget), CombatEvtGlobals.OnAbilityInvalidSpellCast);
                return false;
            }

            if (!selectedTarget.isAlive && !HasFlags(AbFlags.CanDeathTarget))
            {
                if (HasBehavior(AbBehavior.Point)) return true;
                Mgr.Event.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.DeadTarget), CombatEvtGlobals.OnAbilityInvalidSpellCast);
                return false;
            }

            if (HasTargetTeam(AbTargetTeam.Self) && selectedTarget == owner ||
                HasTargetTeam(AbTargetTeam.Enemy) && selectedTarget.unitTeam != owner.unitTeam ||
                HasTargetTeam(AbTargetTeam.Friendly) && owner.unitTeam == selectedTarget.unitTeam)
                return true;

            Mgr.Event.Send<EvtAbilityCastError>(new(this, owner, AbilityGlobals.InvalidTeam), CombatEvtGlobals.OnAbilityInvalidSpellCast);
            return false;
        }
    }
}
