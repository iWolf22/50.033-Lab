using UnityEngine;

public class BrickPowerupController : BlockBounce
{
    [SerializeField] bool breakable = true;

    protected override bool CanBreak(PlayerMovement player) =>
        breakable && player.IsSuperMario && (!spawnCoin || spent);
}
