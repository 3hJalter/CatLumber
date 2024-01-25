using _Game.DesignPattern.StateMachine;
using _Game.GameGrid.Unit.DynamicUnit.Enemy.EnemyStates;
using _Game.GameGrid.Unit.DynamicUnit.Interface;
using _Game.Managers;
using DG.Tweening;
using GameGridEnum;
using _Game.Utilities;
using UnityEngine;

namespace _Game.GameGrid.Unit.DynamicUnit.Enemy
{
    public class MageEnemy : GridUnitDynamic, ICharacter
    {
        [SerializeField] 
        private Animator animator;
        [HideInInspector] 
        public bool IsDead = false;
        private StateMachine<MageEnemy> stateMachine;
        public StateMachine<MageEnemy> StateMachine => stateMachine;
        public override StateEnum CurrentStateId
        {
            get => StateEnum.Idle;
            set => stateMachine.ChangeState(value);
        }

        private string _currentAnim = Constants.INIT_ANIM;
        private bool _isAddState;


        private bool _isWaitAFrame;

        public Direction Direction { get; private set; } = Direction.None;
        public float AnimSpeed => animator.speed;

        private void FixedUpdate()
        {
            if (!GameManager.Ins.IsState(GameState.InGame)) return;
            if (_isWaitAFrame)
            {
                _isWaitAFrame = false;
                //Direction = Direction.None;
                // TEST: Reset the Input if Direction is not none and Move is Swipe (Swipe only take one input per swipe)
                stateMachine?.UpdateState();
                return;
            }

            _isWaitAFrame = true;
            stateMachine?.UpdateState();
        }
        public override void OnInit(GameGridCell mainCellIn, HeightLevel startHeightIn = HeightLevel.One,
            bool isUseInitData = false, Direction skinDirection = Direction.None, bool hasSetPosAndRot = false)
        {
            base.OnInit(mainCellIn, startHeightIn, isUseInitData, skinDirection, hasSetPosAndRot); //DEV: Not use init data
            if (!_isAddState)
            {
                _isAddState = true;
                stateMachine = new StateMachine<MageEnemy>(this);
                AddState();
            }
            IsDead = false;
            Direction = Direction.None;
            stateMachine.ChangeState(StateEnum.Idle);
        }
        public override void OnBePushed(Direction direction = Direction.None, GridUnit pushUnit = null)
        {
            if (pushUnit is Player.Player player)
            {
                LookDirection(Constants.InvDirection[direction]);
                stateMachine.ChangeState(StateEnum.Idle);
                player.CheckingStunState();
                return;
            }
            IsDead = true;
        }
        private void AddState()
        {
            stateMachine.AddState(StateEnum.Idle, new IdleMageEnemyState());
            stateMachine.AddState(StateEnum.Attack, new AttackMageEnemyState());
            stateMachine.AddState(StateEnum.Die, new DieMageEnemyState());
        }
        public void ChangeAnim(string animName, bool forceAnim = false)
        {
            if (!forceAnim)
                if (_currentAnim.Equals(animName))
                    return;
            animator.ResetTrigger(_currentAnim);
            _currentAnim = animName;
            animator.SetTrigger(_currentAnim);
        }
        public void SetAnimSpeed(float speed)
        {
            animator.speed = speed;
        }
        public bool IsCurrentAnimDone()
        {
            return animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1;
        }

        private void LookDirection(Direction directionIn)
        {
            if (directionIn is Direction.None) return;
            skinRotationDirection = directionIn;
            skin.DOLookAt(Tf.position + Constants.DirVector3[directionIn], 0.2f, AxisConstraint.Y, Vector3.up);
        }

        public void OnCharacterDie()
        {
            DevLog.Log(DevId.Hoang, "TODO: Archer Die Logic");
            IsDead = true;
            stateMachine.ChangeState(StateEnum.Die);
        }
    }
}