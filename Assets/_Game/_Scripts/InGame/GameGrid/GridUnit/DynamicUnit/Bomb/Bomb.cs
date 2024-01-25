using System;
using System.Collections.Generic;
using _Game._Scripts.InGame.GameCondition.Data;
using _Game.DesignPattern.ConditionRule;
using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.DynamicUnit.Bomb.BombState;
using _Game.GameGrid.Unit.DynamicUnit.Interface;
using _Game.Utilities.Timer;
using a;
using GameGridEnum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Bomb 
{
    public class Bomb : GridUnitDynamic, IExplosives
    {
        [Title("Bomb")]
        [SerializeField] private GameObject waitExplosionObjectEffect;
        
        private StateMachine<Bomb> _stateMachine;
        
        public StateMachine<Bomb> StateMachine => _stateMachine;
        
        private bool _isAddState;

        private bool _isWaitForExplode;
        
        private const int TIK_BEFORE_EXPLODE = 5; // 1 Tick = 0.5f
        private const float TIME_PER_TIK = 0.5f;
        private readonly List<float> times = new();
        private readonly List<Action> actions = new();
        private STimer timer;
        
        public override void OnInit(GameGridCell mainCellIn, HeightLevel startHeightIn = HeightLevel.One, bool isUseInitData = true,
            Direction skinDirection = Direction.None, bool hasSetPosAndRos = false)
        {
            base.OnInit(mainCellIn, startHeightIn, isUseInitData, skinDirection, hasSetPosAndRos);
            if (!_isAddState)
            {
                _isAddState = true;
                _stateMachine = new StateMachine<Bomb>(this);
                AddState();
            }
            _stateMachine.ChangeState(StateEnum.Idle);
        }
        
        public override void OnDespawn()
        {
            if (_isWaitForExplode) StopExplode();
            StateMachine.OverrideState = StateEnum.None;
            waitExplosionObjectEffect.SetActive(false);
            _stateMachine.ChangeState(StateEnum.Idle);
            base.OnDespawn();
        }
        
        public override void OnPush(Direction direction, ConditionData conditionData = null)
        {
            if (conditionData is not MovingData movingData) return;
            for (int i = 0; i < movingData.blockDynamicUnits.Count; i++) movingData.blockDynamicUnits[i].OnBePushed(direction, this);
        }
        
        public override void OnBePushed(Direction direction = Direction.None, GridUnit pushUnit = null)
        {
            BeInteractedData.SetData(direction, pushUnit);
            if (!ConditionMergeOnBeInteracted.IsApplicable(BeInteractedData)) return;
            
            base.OnBePushed(direction, pushUnit);
            _stateMachine.ChangeState(StateEnum.Roll);
        }
        
        public void StartWaitForExplode()
        {
            if (_isWaitForExplode) return;
            _isWaitForExplode = true;
            timer = TimerManager.Inst.WaitForTime(times, actions);
        }
        
        public override bool IsCurrentStateIs(StateEnum stateEnum)
        {
            return _stateMachine.CurrentState.Id == stateEnum;
        }
        
        private void AddState()
        {
            _stateMachine.AddState(StateEnum.Idle, new IdleBombState());
            _stateMachine.AddState(StateEnum.Fall, new FallBombState());
            _stateMachine.AddState(StateEnum.Roll, new RollBombState());
            _stateMachine.AddState(StateEnum.Explode, new ExplodeBombState());
            _stateMachine.AddState(StateEnum.RollBlock, new RollBlockBombState());
            
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
        
        private void ChangeWaitExplosionObjectEffect()
        {
            waitExplosionObjectEffect.SetActive(!waitExplosionObjectEffect.activeSelf);
        }

        public void Explode()
        {
            if (_stateMachine.CurrentState.Id == StateEnum.Explode) return;
            StateMachine.OverrideState = StateEnum.Explode;
            StateMachine.ChangeState(StateEnum.Explode);
        }
        
        public override StateEnum CurrentStateId 
        { 
            get => _stateMachine?.CurrentStateId ?? StateEnum.Idle;
            set => _stateMachine.ChangeState(value);
        }
        
        protected override void OnOutTriggerBelow(GridUnit triggerUnit)
        {
            base.OnOutTriggerBelow(triggerUnit);
            if (!LevelManager.Ins.IsConstructingLevel)  
                _stateMachine.ChangeState(StateEnum.Fall);
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
                save = new ExplosiveBombMemento(this, CurrentStateId, isSpawn, Tf.position, skin.rotation, startHeight, endHeight
                    , unitTypeY, unitTypeXZ, belowUnits, neighborUnits, upperUnits, mainCell, cellInUnits, islandID, lastPushedDirection);
            }
            return save;
        }

        private class ExplosiveBombMemento : DynamicUnitMemento<Bomb>
        {
            public ExplosiveBombMemento(GridUnitDynamic main, StateEnum currentState, params object[] data) : base(main, currentState, data)
            {
            }
            
            public override void Restore()
            {
                base.Restore();
                if (main._isWaitForExplode) main.StopExplode();
            }
        }

        #region Ruling

        [SerializeField] private ConditionMerge conditionMergeOnBePushed;
        [SerializeField] private ConditionMerge conditionMergeOnBeInteracted;
        public ConditionMerge ConditionMergeOnBePushed => conditionMergeOnBePushed;
        private ConditionMerge ConditionMergeOnBeInteracted => conditionMergeOnBeInteracted;

        private MovingData _movingData;
        private BeInteractedData _beInteractedData;
        
        public MovingData MovingData => _movingData ??= new MovingData(this);
        public BeInteractedData BeInteractedData => _beInteractedData ??= new BeInteractedData(this);

        #endregion
    }
}