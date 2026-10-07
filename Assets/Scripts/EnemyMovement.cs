using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    // Event + delegate for stomping (score is handled by subscriber)
    public delegate void GoombaStompedHandler();
    public event GoombaStompedHandler OnGoombaStomped;

    private bool isDead = false;

    private float originalX;
    private float maxOffset = 5.0f;
    private float enemyPatroltime = 2.0f;
    private int moveRight = -1;
    private Vector2 velocity;

    private Rigidbody2D enemyBody;
    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider;
    private Sprite originalSprite;
    private Vector3 originalScale;
    public float stompHideDelay = 0.5f;

    [HideInInspector]
    public Vector3 startPosition;

    void Start()
    {
        enemyBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
        }
        originalScale = transform.localScale;
        // get the starting position
        // capture scene/prefab-instance placement (Goomba is child under Enemies),
        // so hardcoded Inspector value can't go stale
        startPosition = transform.position;
        originalX = transform.position.x;
        ComputeVelocity();
    }
    void ComputeVelocity()
    {
        velocity = new Vector2((moveRight) * maxOffset / enemyPatroltime, 0);
    }
    void Movegoomba()
    {
        enemyBody.MovePosition(enemyBody.position + velocity * Time.fixedDeltaTime);
    }

    // note that this is Update(), which still works but not ideal. See below.
    void Update()
    {
        if (isDead) return;
        if (Mathf.Abs(enemyBody.position.x - originalX) < maxOffset)
        {// move goomba
            Movegoomba();
        }
        else
        {
            // change direction
            moveRight *= -1;
            ComputeVelocity();
            Movegoomba();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log(other.gameObject.name);
    }

    // Called when Mario stomps this Goomba: notify subscribers, then disappear
    public void Stomp()
    {
        if (isDead) return;
        isDead = true;
        OnGoombaStomped?.Invoke();
        // Code-only stomp animation: swap to dead-goombas sprite for a split second
        if (enemyCollider != null) enemyCollider.enabled = false;
        if (spriteRenderer != null)
        {
            Sprite dead = null;
            foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (s.name == "dead-goombas") { dead = s; break; }
            }
            if (dead != null) spriteRenderer.sprite = dead;
            else transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.5f, originalScale.z);
        }
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(stompHideDelay);
        gameObject.SetActive(false);
    }

    public void ResetEnemy()
    {
        StopAllCoroutines();
        isDead = false;
        transform.position = startPosition;
        transform.localScale = originalScale;
        if (spriteRenderer != null && originalSprite != null) spriteRenderer.sprite = originalSprite;
        if (enemyCollider != null) enemyCollider.enabled = true;
        gameObject.SetActive(true);
    }
}