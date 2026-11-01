using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Shared assets carry events across scenes; listeners remain scene-local.
public abstract class GameEvent<T> : ScriptableObject
{
    readonly List<GameEventListener<T>> listeners = new();
    event UnityAction<T> raised;

    public void RegisterListener(GameEventListener<T> listener)
    {
        if (!listeners.Contains(listener)) listeners.Add(listener);
    }
    public void UnregisterListener(GameEventListener<T> listener) => listeners.Remove(listener);
    public void AddListener(UnityAction<T> listener) => raised += listener;
    public void RemoveListener(UnityAction<T> listener) => raised -= listener;

    public void Raise(T value)
    {
        // Responses may disable UI and unregister listeners during dispatch.
        var snapshot = listeners.ToArray();
        foreach (var listener in snapshot)
            if (listener != null && listener.isActiveAndEnabled) listener.OnEventRaised(value);
        raised?.Invoke(value);
    }
}
