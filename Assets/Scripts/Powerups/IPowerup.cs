using UnityEngine;

public enum PowerupType { Coin, MagicMushroom, OneUpMushroom, StarMan, FireFlower, Damage = 99, Default = -1 }

public interface IPowerup
{
    PowerupType powerupType { get; }
    bool hasSpawned { get; }
    void DestroyPowerup();
    void SpawnPowerup();
    void ApplyPowerup(MonoBehaviour target);
}

public interface IPowerupApplicable
{
    void RequestPowerupEffect(IPowerup powerup);
}
