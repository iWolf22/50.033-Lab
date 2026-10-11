using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class EnemyMovement : MonoBehaviour
{
    public event Action OnGoombaStomped;
    [SerializeField] float speed = 1.8f;
    [SerializeField] float patrolRadius = 2f;
    [SerializeField] Sprite stompedSprite;
    public float stompHideDelay = 0.5f;
    Rigidbody2D body;
    SpriteRenderer visual;
    Vector2 startPosition;
    Sprite initialSprite;
    float startX;
    int direction = -1;
    bool dead;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>();
        startPosition = body.position;
        initialSprite = visual.sprite;
        startX = body.position.x;
    }

    void FixedUpdate()
    {
        if (dead) return;
        if ((body.position.x - startX) * direction >= patrolRadius) direction *= -1;
        body.MovePosition(body.position + Vector2.right * (direction * speed * Time.fixedDeltaTime));
    }

    public void Stomp()
    {
        if (dead) return;
        dead = true;
        GetComponent<Collider2D>().enabled = false;
        body.simulated = false;
        if (TryGetComponent<Animator>(out var animator)) animator.enabled = false;
        float height = visual.sprite.bounds.size.y;
        visual.sprite = stompedSprite;
        transform.position += Vector3.down * ((height - stompedSprite.bounds.size.y) / 2);
        OnGoombaStomped?.Invoke();
        StartCoroutine(Hide());
    }

    public void GameRestart()
    {
        StopAllCoroutines();
        dead = false;
        direction = -1;
        enabled = true;
        gameObject.SetActive(true);
        body.simulated = true;
        body.position = startPosition;
        transform.position = startPosition;
        visual.sprite = initialSprite;
        GetComponent<Collider2D>().enabled = true;
        if (TryGetComponent<Animator>(out var animator))
        {
            animator.enabled = true;
            animator.Rebind(); animator.Update(0);
        }
    }

    IEnumerator Hide()
    {
        yield return new WaitForSeconds(stompHideDelay);
        gameObject.SetActive(false);
    }
}
