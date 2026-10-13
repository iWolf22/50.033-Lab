using UnityEngine;

[CreateAssetMenu(fileName = "GameConstants", menuName = "ScriptableObjects/GameConstants", order = 1)]
public class GameConstants : ScriptableObject
{
    public int speed = 45;
    public int maxSpeed = 6;
    public int upSpeed = 15;
    public int deathImpulse = 12;
}
