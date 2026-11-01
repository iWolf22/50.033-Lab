using UnityEngine;
[CreateAssetMenu(menuName = "Pluggable FSM/Decisions/Countdown")]
public class CountdownDecision : Decision
{
    public float duration = 5;
    public override bool Decide(StateController controller) => controller.stateTimeElapsed >= duration;
}
