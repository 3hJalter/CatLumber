namespace _Game.DesignPattern.StateMachine
{
    public static class StateUtilities
    {
        public static void ChangeState<T>(ref IState<T> currentState, T owner, IState<T> newState) where T : class
        {
            currentState.OnExit(owner);
            newState.OnEnter(owner);
            currentState = newState;
        }
    }
}
