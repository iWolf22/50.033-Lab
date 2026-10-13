public class MagicMushroomPowerup : MovingPowerup
{
    public override PowerupType powerupType => PowerupType.MagicMushroom;
    protected override string CollectionMessage => "mushroom collected";
}
