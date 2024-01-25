using _Game.GameGrid.Unit;
using _Game.Utilities;
using System.Collections.Generic;

namespace _Game.DesignPattern.StateMachine
{
    public class StateMachine<T> where T : GridUnit
    {
        private readonly Dictionary<StateEnum, IState<T>> states;
        public IState<T> CurrentState { get; private set; }
        public StateEnum CurrentStateId => CurrentState?.Id ?? StateEnum.None;
        private readonly T main;
        public StateEnum OverrideState;
        public bool Debug = false;
        public StateMachine(T main)
        {
            states = new Dictionary<StateEnum, IState<T>> ();
            this.main = main;
            OverrideState = StateEnum.None;
        }
        public void AddState(StateEnum id, IState<T> state)
        {
            if(states.ContainsKey(id))
            {
                states[id] = state;
            }
            else
            {
                states.Add(id, state);
            }
        }
        public void RemoveState(StateEnum id)
        {
            if (states.ContainsKey(id))
            {
                states[id] = null;
            }
        }
        public void ChangeState(StateEnum id)
        {
            if(id == StateEnum.None)
            {
                CurrentState = null;
                return;
            }

            if (OverrideState != StateEnum.None && OverrideState != id) return;
            if (Debug)
            {
                DevLog.Log(DevId.Hung, $"{CurrentState?.Id} -> {id}");
            }
            CurrentState?.OnExit(main);
            CurrentState = states[id];
            CurrentState.OnEnter(main);
        }
        public void UpdateState()
        {
            CurrentState.OnExecute(main);
        }
    }
}