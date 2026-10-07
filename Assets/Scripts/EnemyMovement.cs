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

    [HideInInspector]
    public Vector3 startPosition;

    void Start()
    {
        enemyBody = GetComponent<Rigidbody2D>();
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
        gameObject.SetActive(false);
    }

    public void ResetEnemy()
    {
        isDead = false;
        transform.position = startPosition;
        gameObject.SetActive(true);
    }
}