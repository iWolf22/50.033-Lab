using UnityEngine;

public enum MarioForm { Small, Super, Fire, Hurt, Dead }

[CreateAssetMenu(menuName = "Pluggable FSM/Actions/Mario Form")]
public class MarioFormAction : StateAction
{
    public MarioForm form;
    public RuntimeAnimatorController animatorController;
    public AudioClip sound;
    public AudioClip damageSound;
    public float damageProtectionDuration;
    public override void Act(StateController controller)
    {
        var mario = (MarioStateController)controller;
        bool damaged = controller.PendingPowerup == PowerupType.Damage;
        mario.SetForm(form, animatorController, damaged && damageSound != null ? damageSound : sound);
        if (damaged && damageProtectionDuration > 0) mario.ProtectFromDamage(damageProtectionDuration);
        controller.ClearPowerup();
    }
}
