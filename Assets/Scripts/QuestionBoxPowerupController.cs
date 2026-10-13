public class QuestionBoxPowerupController : BlockBounce
{
    public override void Disable()
    {
        base.Disable();
        if (springBody != null) springBody.bodyType = UnityEngine.RigidbodyType2D.Static;
    }
}
