using UnityEngine;

public enum MarioForm { Small, Super, Fire, Hurt, Dead }

[CreateAssetMenu(menuName = "Pluggable FSM/Actions/Mario Form")]
public class MarioFormAction : StateAction
{
    public MarioForm form;
    public RuntimeAnimatorController animatorController;
    public AudioClip sound;
    public override void Act(StateController controller)
    {
        ((MarioStateController)controller).SetForm(form, animatorController, sound);
        controller.ClearPowerup();
    }
}
