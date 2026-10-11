using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    public float speed = 45;
    public float maxSpeed = 6;
    public float upSpeed = 15;
    public float deathImpulse = 12;
    public Animator marioAnimator;
    public AudioSource marioAudio;
    public AudioSource deathAudio;
    public AudioClip marioDeath;
    [SerializeField] GameManager session;
    [SerializeField] LayerMask groundLayers;
    [SerializeField] float fallLimit = -12;
    Rigidbody2D body;
    SpriteRenderer visual;
    Collider2D hitbox;
    ContactFilter2D groundFilter;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
    Vector2 startPosition;
    int movement;
    bool jumpQueued;
    bool holdQueued;
    bool jumped;
    bool grounded;
    public bool alive { get; private set; } = true;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        startPosition = body.position;
        visual = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<Collider2D>();
        groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = groundLayers };
    }

    public void MoveCheck(int value)
    {
        movement = value;
        if (!alive || value == 0) return;
        if (body.linearVelocity.x * value < -0.1f) marioAnimator.SetTrigger("onSkid");
        visual.flipX = value < 0;
    }

    public void Jump() => jumpQueued = alive;
    public void JumpHold() => holdQueued = alive;

    void FixedUpdate()
    {
        if (!alive) return;
        grounded = false;
        int count = hitbox.Cast(Vector2.down, groundFilter, groundHits, 0.06f);
        for (int i = 0; i < count; i++)
            grounded |= groundHits[i].normal.y > 0.5f && body.linearVelocity.y <= 0.1f;
        if (grounded) jumped = false;
        Vector2 velocity = body.linearVelocity;
        velocity.x = Mathf.MoveTowards(velocity.x, movement * maxSpeed, speed * Time.fixedDeltaTime);
        if (jumpQueued && grounded)
        {
            velocity.y = upSpeed;
            grounded = false;
            jumped = true;
        }
        if (holdQueued && jumped && velocity.y > 0)
        {
            velocity.y += upSpeed * 0.3f;
            jumped = false;
        }
        body.linearVelocity = velocity;
        jumpQueued = holdQueued = false;
        if (body.position.y < fallLimit) Die();
    }

    void Update()
    {
        if (!alive) return;
        marioAnimator.SetFloat("xSpeed", Mathf.Abs(body.linearVelocity.x));
        marioAnimator.SetBool("onGround", grounded);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!alive || !other.TryGetComponent<EnemyMovement>(out var enemy)) return;
        float feet = hitbox.bounds.min.y;
        if (body.linearVelocity.y < 0 && feet >= other.bounds.max.y - 0.25f)
        {
            enemy.Stomp();
            body.linearVelocity = new Vector2(body.linearVelocity.x, upSpeed * 0.6f);
            grounded = false;
        }
        else Die();
    }

    void Die()
    {
        alive = false;
        movement = 0;
        jumpQueued = holdQueued = false;
        hitbox.enabled = false;
        body.linearVelocity = Vector2.zero;
        marioAnimator.ResetTrigger("onSkid");
        marioAnimator.Play("mario-die");
        deathAudio.PlayOneShot(marioDeath);
    }

    // Called by the jump and death animation clips.
    void PlayJumpSound()
    {
        if (alive && body.linearVelocity.y > 0) marioAudio.PlayOneShot(marioAudio.clip);
    }
    void PlayDeathImpulse() => body.AddForce(Vector2.up * deathImpulse, ForceMode2D.Impulse);
    void GameOverScene() => session.GameOver();
    public void GameRestart()
    {
        alive = true;
        movement = 0;
        jumpQueued = holdQueued = jumped = grounded = false;
        body.position = startPosition;
        transform.position = startPosition;
        body.linearVelocity = Vector2.zero;
        hitbox.enabled = true;
        visual.flipX = false;
        marioAudio.Stop(); deathAudio.Stop();
        marioAnimator.Rebind(); marioAnimator.Update(0);
    }
}
