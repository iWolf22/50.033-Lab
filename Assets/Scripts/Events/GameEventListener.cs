using UnityEngine;
using UnityEngine.Events;

public abstract class GameEventListener<T> : MonoBehaviour
{
    public GameEvent<T> Event;
    public UnityEvent<T> Response = new();
    protected virtual void OnEnable() => Event.RegisterListener(this);
    protected virtual void OnDisable() => Event.UnregisterListener(this);
    public void OnEventRaised(T data) => Response.Invoke(data);
}
