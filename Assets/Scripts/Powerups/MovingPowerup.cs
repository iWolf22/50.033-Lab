using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public abstract class MovingPowerup : BasePowerup
{
    [SerializeField] float moveSpeed = 3;
    [SerializeField] PowerupGameEvent collected;
    bool goRight;
    protected abstract string CollectionMessage { get; }

    public override void SpawnPowerup()
    {
        if (hasSpawned) return;
        gameObject.SetActive(true);
        hasSpawned = true;
        goRight = true;
        StartCoroutine(Emerge());
    }

    IEnumerator Emerge()
    {
        Vector3 origin = transform.localPosition;
        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            transform.localPosition = origin + Vector3.up * (t / 0.5f);
            yield return null;
        }
        transform.localPosition = origin + Vector3.up;
        rigidBody.simulated = true;
    }

    protected void FixedUpdate()
    {
        if (hasSpawned && !consumed && rigidBody.simulated)
            rigidBody.linearVelocity = new Vector2(powerupType == PowerupType.FireFlower ? 0 : (goRight ? moveSpeed : -moveSpeed), rigidBody.linearVelocity.y);
    }

    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hasSpawned || consumed) return;
        if (collision.gameObject.TryGetComponent<PlayerMovement>(out var player) && player.alive)
        {
            player.RequestPowerupEffect(this);
            DestroyPowerup();
            return;
        }
        for (int i = 0; i < collision.contactCount; i++)
            if (Mathf.Abs(collision.GetContact(i).normal.x) > 0.5f)
            {
                goRight = collision.GetContact(i).normal.x > 0;
                break;
            }
    }

    public override void ApplyPowerup(MonoBehaviour target)
    {
        if (consumed || target is not PlayerMovement player || !player.alive) return;
        consumed = true;
        Debug.Log(CollectionMessage, this);
        collected.Raise(this);
    }
}
