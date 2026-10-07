public class StateMachine<T>
{
    private T owner;

    public IState<T> CurrentState { get; private set; }
    public IState<T> PreviousState { get; private set; }

    public StateMachine(T owner)
    {
        this.owner = owner;
    }

    public void Update()
    {
        CurrentState?.Update(owner);
    }

    public void ChangeState(IState<T> nextState)
    {
        if (nextState == null || CurrentState == nextState)
        {
            return;
        }

        CurrentState?.Exit(owner);

        PreviousState = CurrentState;
        CurrentState = nextState;

        CurrentState.Enter(owner);
    }
}