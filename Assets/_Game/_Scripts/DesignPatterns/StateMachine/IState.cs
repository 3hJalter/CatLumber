namespace _Game.DesignPattern.StateMachine
{
    public interface IState<in T>
    {
        public StateEnum Id
        {
            get;
        }
        void OnEnter(T t);
        void OnExecute(T t);
        void OnExit(T t);
    }
}
