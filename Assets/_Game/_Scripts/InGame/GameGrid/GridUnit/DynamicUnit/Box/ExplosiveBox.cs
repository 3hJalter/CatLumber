using System;
using System.Collections.Generic;
using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.DynamicUnit.Box.BoxState;
using _Game.GameGrid.Unit.DynamicUnit.Interface;
using _Game.Utilities.Timer;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Box
{
    public class ExplosiveBox : Box, IExplosives
    {
        [Title("Explosive Box")]
        [SerializeField] private GameObject waitExplosionObjectEffect;

        private bool _isWaitForExplode;
        
        private const int TIK_BEFORE_EXPLODE = 5; // 1 Tick = 0.5f
        private const float TIME_PER_TIK = 0.5f;
        private readonly List<float> times = new();
        private readonly List<Action> actions = new();
        private STimer timer;

        private void ChangeWaitExplosionObjectEffect()
        {
            waitExplosionObjectEffect.SetActive(!waitExplosionObjectEffect.activeSelf);
        }

        public void Explode()
        {
            if (StateMachine.CurrentState.Id == StateEnum.Explode) return;
            StateMachine.OverrideState = StateEnum.Explode;
            StateMachine.ChangeState(StateEnum.Explode);
        }

        protected override void AddState()
        {
            base.AddState();
            StateMachine.AddState(StateEnum.Explode, new ExplodeBoxState());

            #region Set Timer for Explode State

            for (int i = 0; i < TIK_BEFORE_EXPLODE; i++)
            {
                times.Add(TIME_PER_TIK * (i+1));
                actions.Add(ChangeWaitExplosionObjectEffect);
            }
            times.Add(TIME_PER_TIK * (TIK_BEFORE_EXPLODE + 1));
            actions.Add(Explode);

            #endregion
        }

        public override void OnBePushed(Direction direction = Direction.None, GridUnit pushUnit = null)
        {
            base.OnBePushed(direction, pushUnit);
            if (_isWaitForExplode) return;
            if (pushUnit is not Player.Player) return;
            _isWaitForExplode = true;
            timer = TimerManager.Inst.WaitForTime(times, actions);
        }

        public override void OnDespawn()
        {
            if (_isWaitForExplode) StopExplode();
            StateMachine.OverrideState = StateEnum.None;
            waitExplosionObjectEffect.SetActive(false);
            base.OnDespawn();
        }
        
        public void StopExplode()
        {
            _isWaitForExplode = false;
            TimerManager.Inst.StopTimer(ref timer);
            // Timing.KillCoroutines(EXPLODE_TAG);
            waitExplosionObjectEffect.SetActive(false);
            StateMachine.OverrideState = StateEnum.None;
        }
        
        public override IMemento Save()
        {
            IMemento save;
            if (overrideSpawnSave != null)
            {
                save = overrideSpawnSave;
                overrideSpawnSave = null;
            }
            else
            {
                save = new ExplosiveBoxMemento(this, CurrentStateId, isSpawn, Tf.position, skin.rotation, startHeight, endHeight
                    , unitTypeY, unitTypeXZ, belowUnits, neighborUnits, upperUnits, mainCell, cellInUnits, islandID, lastPushedDirection);
            }
            return save;
        }

        private class ExplosiveBoxMemento : DynamicUnitMemento<ExplosiveBox>
        {
            public ExplosiveBoxMemento(GridUnitDynamic main, StateEnum currentState, params object[] data) : base(main, currentState, data)
            {
            }
            
            public override void Restore()
            {
                base.Restore();
                if (main._isWaitForExplode) main.StopExplode();
            }
        }
    }
}
