using UnityEngine;
[CreateAssetMenu(menuName = "Pluggable FSM/Actions/Fire Attack")]
public class FireAttackAction : StateAction
{
    public FireballController fireballPrefab;
    public override void Act(StateController controller) => ((MarioStateController)controller).FireProjectile(fireballPrefab);
}
