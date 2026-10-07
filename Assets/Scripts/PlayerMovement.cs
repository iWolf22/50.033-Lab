using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 10;
    private Rigidbody2D marioBody;

    private SpriteRenderer marioSprite;
    private bool faceRightState = true;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI finalScoreText;

    public GameObject enemies;

    [HideInInspector]
    public Vector3 startPosition;

    public GameObject gamePlayUI;
    public GameObject gameOverUI;

    public Animator marioAnimator;

    public JumpOverGoomba jumpOverGoomba;

    // Start is called before the first frame update
    void Start()
    {
        marioSprite = GetComponent<SpriteRenderer>();
        startPosition = transform.position;

        // Set to be 30 FPS
        Application.targetFrameRate = 30;
        marioBody = GetComponent<Rigidbody2D>();

        gameOverUI.SetActive(false);
        gamePlayUI.SetActive(true);

        marioAnimator.SetBool("onGround", onGroundState);

        // Jump-over scoring is disabled; stomping scores via event instead
        if (jumpOverGoomba != null) jumpOverGoomba.enabled = false;

        // Subscribe to each Goomba's stomp event to increase score
        foreach (Transform eachChild in enemies.transform)
        {
            EnemyMovement enemy = eachChild.GetComponent<EnemyMovement>();
            if (enemy != null) enemy.OnGoombaStomped += OnGoombaStomped;
        }
    }
    void FlipMarioSprite(int value)
    {
        if (value == -1 && faceRightState)
        {
            faceRightState = false;
            marioSprite.flipX = true;
            if (marioBody.linearVelocity.x > 0.05f)
                marioAnimator.SetTrigger("onSkid");

        }

        else if (value == 1 && !faceRightState)
        {
            faceRightState = true;
            marioSprite.flipX = false;
            if (marioBody.linearVelocity.x < -0.05f)
                marioAnimator.SetTrigger("onSkid");
        }
    }

    private bool moving = false;

    void Move(int value)
    {

        Vector2 movement = new Vector2(value, 0);
        // check if it doesn't go beyond maxSpeed
        if (marioBody.linearVelocity.magnitude < maxSpeed)
            marioBody.AddForce(movement * speed);
    }

    public void MoveCheck(int value)
    {
        if (value == 0)
        {
            moving = false;
        }
        else
        {
            FlipMarioSprite(value);
            moving = true;
            Move(value);
        }
    }



    // Update is called once per frame
    void Update()
    {
        marioAnimator.SetFloat("xSpeed", Mathf.Abs(marioBody.linearVelocity.x));
    }

    public float maxSpeed = 20;

    public float upSpeed = 10;
    private bool onGroundState = true;
    private bool jumpedState = false;

    public void Jump()
    {
        if (alive && onGroundState)
        {
            marioBody.AddForce(Vector2.up * upSpeed, ForceMode2D.Impulse);
            onGroundState = false;
            jumpedState = true;
            marioAnimator.SetBool("onGround", onGroundState);
        }
    }

    public void JumpHold()
    {
        if (alive && jumpedState)
        {
            marioBody.AddForce(Vector2.up * upSpeed * 30, ForceMode2D.Force);
            jumpedState = false;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if ((col.gameObject.CompareTag("Ground") || col.gameObject.CompareTag("Obstacles") || col.gameObject.CompareTag("Enemy")) && !onGroundState)
        {
            onGroundState = true;
            marioAnimator.SetBool("onGround", onGroundState);
        }
    }

    // FixedUpdate may be called once per frame. See documentation for details.
    void FixedUpdate()
    {
        if (alive && moving)
        {
            Move(faceRightState == true ? 1 : -1);
        }
    }


    public Transform gameCamera;

    public AudioClip marioDeath;
    public float deathImpulse = 15;

    // state
    [System.NonSerialized]
    public bool alive = true;


    void PlayDeathImpulse()
    {
        marioBody.AddForce(Vector2.up * deathImpulse, ForceMode2D.Impulse);
    }

    void GameOverScene()
    {
        // stop time
        Time.timeScale = 0.0f;
        // set gameover scene
        GameOver();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Enemy") && alive)
        {
            EnemyMovement enemy = other.GetComponent<EnemyMovement>();
            // Stomp: Mario is above the Goomba and falling -> kill Goomba, bounce Mario
            if (enemy != null && transform.position.y > enemy.transform.position.y + 0.3f
                && marioBody.linearVelocity.y <= 0.1f)
            {
                enemy.Stomp();
                marioBody.linearVelocity = new Vector2(marioBody.linearVelocity.x, 0);
                marioBody.AddForce(Vector2.up * upSpeed, ForceMode2D.Impulse);
                onGroundState = false;
                marioAnimator.SetBool("onGround", onGroundState);
                return;
            }

            PlayDeathImpulse();
            marioAnimator.Play("mario-die");
            marioAudio.PlayOneShot(marioDeath);
            alive = false;
        }
    }

    public AudioSource goombaDeath;

    // Event handler: Goomba stomped -> increase score
    void OnGoombaStomped()
    {
        goombaDeath.PlayOneShot(goombaDeath.clip);
        jumpOverGoomba.score++;
        scoreText.text = "Score: " + jumpOverGoomba.score;
    }

    private void GameOver()
    {
        Debug.Log("Collided with goomba!");

        gameOverUI.SetActive(true);
        gamePlayUI.SetActive(false);

        finalScoreText.text = "Score: " + jumpOverGoomba.score;
    }

    public void RestartButtonCallback(int input)
    {
        Debug.Log("Restart!");
        // reset everything
        ResetGame();
        // resume time
        Time.timeScale = 1.0f;
    }

    private void ResetGame()
    {
        // reset position
        marioBody.transform.position = startPosition;
        // reset sprite direction
        faceRightState = true;
        marioSprite.flipX = false;
        // reset score
        scoreText.text = "Score: 0";
        // reset Goomba(s) - Goomba(s) are children under Enemies entity
        foreach (Transform eachChild in enemies.transform)
        {
            EnemyMovement enemy = eachChild.GetComponent<EnemyMovement>();
            if (enemy != null) enemy.ResetEnemy();
            else eachChild.transform.position = startPosition;
        }

        // reset score
        jumpOverGoomba.score = 0;

        gameOverUI.SetActive(false);
        gamePlayUI.SetActive(true);

        marioAnimator.SetTrigger("gameRestart");
        alive = true;

        // reset camera position
        gameCamera.position = new Vector3(0, 0, -10);
    }

    // for audio
    public AudioSource marioAudio;

    void PlayJumpSound()
    {
        // play jump sound
        marioAudio.PlayOneShot(marioAudio.clip);
    }
}