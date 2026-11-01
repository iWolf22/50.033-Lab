using UnityEngine;
[CreateAssetMenu(menuName = "Pluggable FSM/Actions/Star")]
public class StarAction : StateAction
{
    public bool active;
    public override void Act(StateController controller)
    {
        ((BuffStateController)controller).SetStar(active);
        controller.ClearPowerup();
    }
}
