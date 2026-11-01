using UnityEngine;

[CreateAssetMenu(menuName = "Pluggable FSM/State")]
public class State : ScriptableObject
{
    public StateAction[] setupActions = {};
    public StateAction[] actions = {};
    public StateAction[] exitActions = {};
    public StateAction[] attackActions = {};
    public Transition[] transitions = {};
    public void Enter(StateController controller) { foreach (var action in setupActions) action.Act(controller); }
    public void Exit(StateController controller) { foreach (var action in exitActions) action.Act(controller); }
    public void Attack(StateController controller) { foreach (var action in attackActions) action.Act(controller); }
    public void Tick(StateController controller)
    {
        foreach (var action in actions) action.Act(controller);
        foreach (var transition in transitions)
        {
            var next = transition.decision.Decide(controller) ? transition.trueState : transition.falseState;
            if (controller.TransitionToState(next)) break;
        }
    }
}

[System.Serializable]
public class Transition
{
    public Decision decision;
    public State trueState;
    public State falseState;
}
