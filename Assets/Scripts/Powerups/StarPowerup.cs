public class StarPowerup : MovingPowerup
{
    public override PowerupType powerupType => PowerupType.StarMan;
    protected override string CollectionMessage => "star collected";
}
