using UnityEngine;

public abstract class StateController : MonoBehaviour
{
    public State startState;
    public State currentState;
    public State previousState;
    public State remainState;
    public float stateTimeElapsed { get; private set; }
    public PowerupType PendingPowerup { get; protected set; } = PowerupType.Default;

    protected virtual void Start() { if (currentState == null) GameRestart(); }
    protected virtual void Update()
    {
        if (Time.timeScale == 0 || currentState == null) return;
        stateTimeElapsed += Time.deltaTime;
        currentState.Tick(this);
    }
    public bool TransitionToState(State next)
    {
        if (next == null || next == remainState || next == currentState) return false;
        if (currentState != null) currentState.Exit(this);
        previousState = currentState;
        currentState = next;
        stateTimeElapsed = 0;
        currentState.Enter(this);
        return true;
    }
    public virtual void GameRestart()
    {
        if (currentState != null) currentState.Exit(this);
        previousState = currentState;
        currentState = null;
        PendingPowerup = PowerupType.Default;
        TransitionToState(startState);
    }
    public void ClearPowerup() => PendingPowerup = PowerupType.Default;
}
