using System.Collections.Generic;
using System.Linq;
using _Game._Scripts.InGame.GameCondition.Data;
using _Game.DesignPattern.ConditionRule;
using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.DynamicUnit.Interface;
using _Game.GameGrid.Unit.DynamicUnit.Raft.RaftState;
using GameGridEnum;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Raft
{
    public class Raft : GridUnitDynamic, IVehicle
    {
        public Player.Player player;

        public List<GridUnit> blockDirectionUnits = new();

        public Direction rideRaftDirection;

        [SerializeField] private ConditionMerge conditionMergeOnBePushed;

        StateMachine<Raft> stateMachine;
        public StateMachine<Raft> StateMachine => stateMachine;
        public override StateEnum CurrentStateId
        {
            get => StateEnum.Emerge;
            set => stateMachine.ChangeState(value);
        }

        public readonly HashSet<GridUnit> CarryUnits = new();
        private bool _isAddState;
        private bool _isFirstSpawnDone;
        private Direction _lastDirectionSpawn = Direction.None;
        private MovingData _movingData;
        public ConditionMerge ConditionMergeOnBePushed => conditionMergeOnBePushed;
        public MovingData MovingData => _movingData ??= new MovingData(this);

        public void Ride(Direction direction, GridUnit rideUnit)
        {
            rideRaftDirection = direction;
            if (!IsCurrentStateIs(StateEnum.Idle) && rideUnit is Player.Player) return;
            // Check if there is a unit in the direction of the raft           
            stateMachine.ChangeState(StateEnum.Move);
        }

        public override void OnPush(Direction direction, ConditionData conditionData = null)
        {
            List<GridUnitDynamic> blockUnits =
                conditionData is MovingData movingData ? movingData.blockDynamicUnits : new List<GridUnitDynamic>();
            for (int i = 0; i < blockUnits.Count; i++) blockUnits[i].OnBePushed(direction, this);
        }

        public override bool IsCurrentStateIs(StateEnum stateEnum)
        {
            return stateMachine.CurrentState.Id == stateEnum;
        }

        public override void OnInit(GameGridCell mainCellIn, HeightLevel startHeightIn = HeightLevel.One,
            bool isUseInitData = true, Direction skinDirection = Direction.None, bool hasSetPosAndRot = false)
        {
            RotateSize(skinDirection);
            base.OnInit(mainCellIn, startHeightIn, isUseInitData, skinDirection, hasSetPosAndRot);
            if (!_isAddState)
            {
                _isAddState = true;
                stateMachine = new StateMachine<Raft>(this);
                AddState();

            }
            stateMachine.ChangeState(StateEnum.Emerge);
        }

        private void AddState()
        {
            stateMachine.AddState(StateEnum.Idle, new IdleRaftState());
            stateMachine.AddState(StateEnum.Move, new MoveRaftState());
            stateMachine.AddState(StateEnum.Emerge, new EmergeRaftState());
        }

        protected override void OnEnterTriggerUpper(GridUnit triggerUnit)
        {
            if (!(IsCurrentStateIs(StateEnum.Idle) || IsCurrentStateIs(StateEnum.Emerge))) return;
            if (triggerUnit is Player.Player playerIn)
            {
                playerIn.SetVehicle(this);
                player = playerIn;
            }

            CarryUnits.Add(triggerUnit);
        }

        protected override void OnOutTriggerUpper(GridUnit triggerUnit)
        {
            if (!(IsCurrentStateIs(StateEnum.Idle) || IsCurrentStateIs(StateEnum.Emerge))) return;
            if (triggerUnit is Player.Player playerIn)
            {
                playerIn.SetVehicle(null);
                player = null;
            }

            CarryUnits.Remove(triggerUnit);
        }

        private void RotateSize(Direction direction)
        {
            switch (_isFirstSpawnDone)
            {
                case false when direction is Direction.Forward or Direction.Back:
                    size = new Vector3Int(size.z, size.y, size.x);
                    _isFirstSpawnDone = true;
                    break;
                case true when _lastDirectionSpawn != direction:
                    size = new Vector3Int(size.z, size.y, size.x);
                    break;
            }

            _lastDirectionSpawn = direction;
        }

        #region SAVING DATA
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
                save = new RaftMemento(this, player, rideRaftDirection, blockDirectionUnits, CarryUnits, 
                    CurrentStateId, isSpawn, Tf.position, skin.rotation, startHeight, endHeight, unitTypeY, unitTypeXZ, 
                    belowUnits, neighborUnits, upperUnits, mainCell, cellInUnits, islandID, lastPushedDirection);
            }
            return save;
        }

        public class RaftMemento : DynamicUnitMemento<Raft> 
        {
            protected Player.Player player;
            protected Direction rideRaftDirection;
            protected GridUnit[] blockDirectionUnits;
            protected GridUnit[] carryUnit;
            public RaftMemento(GridUnitDynamic main, Player.Player player, Direction rideRaftDirection, List<GridUnit> blockDirectionUnits, HashSet<GridUnit> carryUnit,
                StateEnum currentState, params object[] data) : base(main, currentState, data)
            {
                this.currentState = currentState;
                this.player = player;
                this.rideRaftDirection = rideRaftDirection;
                this.blockDirectionUnits = blockDirectionUnits.ToArray();
                this.carryUnit = carryUnit.ToArray();
            }

            public override void Restore()
            {
                base.Restore();
                main.player = player;
                main.rideRaftDirection = rideRaftDirection;

                main.blockDirectionUnits.Clear();
                for(int i = 0; i < blockDirectionUnits.Length; i++)
                {
                    main.blockDirectionUnits.Add(blockDirectionUnits[i]);
                }

                main.CarryUnits.Clear();
                for (int i = 0; i < carryUnit.Length; i++)
                {
                    main.CarryUnits.Add(carryUnit[i]);
                }
            }
        }
        #endregion
    }
}
