using UnityEngine;

public abstract class BasePowerup : MonoBehaviour, IPowerup
{
    public abstract PowerupType powerupType { get; }
    public bool hasSpawned { get; protected set; }
    protected bool consumed;
    protected Rigidbody2D rigidBody;
    Vector3 startPosition;
    bool initialized;

    protected virtual void Awake() => CacheSpawnState();

    void CacheSpawnState()
    {
        if (initialized) return;
        initialized = true;
        startPosition = transform.localPosition;
        rigidBody = GetComponent<Rigidbody2D>();
    }

    // Keep the object so an in-place restart can restore it.
    public void DestroyPowerup()
    {
        consumed = true;
        gameObject.SetActive(false);
    }

    public virtual void ResetPowerup()
    {
        CacheSpawnState();
        StopAllCoroutines();
        hasSpawned = consumed = false;
        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Vector2.zero;
            rigidBody.angularVelocity = 0;
            rigidBody.simulated = false;
        }
        transform.localPosition = startPosition;
        gameObject.SetActive(false);
    }

    public abstract void SpawnPowerup();
    public abstract void ApplyPowerup(MonoBehaviour target);
}
