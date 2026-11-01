using UnityEngine;
[CreateAssetMenu(menuName = "Pluggable FSM/Decisions/Powerup")]
public class PowerupDecision : Decision
{
    public PowerupType powerup;
    public override bool Decide(StateController controller) => controller.PendingPowerup == powerup;
}
