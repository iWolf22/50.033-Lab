using UnityEngine;

public struct Void { }
[CreateAssetMenu(menuName = "Game Events/Simple")]
public class SimpleGameEvent : GameEvent<Void>
{
    public void Raise() => Raise(default);
}
