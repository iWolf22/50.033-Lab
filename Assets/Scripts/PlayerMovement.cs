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
    }

    // Update is called once per frame
    void Update()
    {
        // toggle state
        if ((Input.GetKeyDown("a") || Input.GetKeyDown(KeyCode.LeftArrow)) && faceRightState)
        {
            faceRightState = false;
            marioSprite.flipX = true;
        }

        if ((Input.GetKeyDown("d") || Input.GetKeyDown(KeyCode.RightArrow)) && !faceRightState)
        {
            faceRightState = true;
            marioSprite.flipX = false;
        }

    }

    public float maxSpeed = 20;

    public float upSpeed = 10;
    private bool onGroundState = true;

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground")) onGroundState = true;
    }

    // FixedUpdate may be called once per frame. See documentation for details.
    void FixedUpdate()
    {
        if (Input.GetKeyDown("space") && onGroundState)
        {
            marioBody.AddForce(Vector2.up * upSpeed, ForceMode2D.Impulse);
            onGroundState = false;
        }

        float moveHorizontal = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(moveHorizontal) > 0)
        {
            Vector2 movement = new Vector2(moveHorizontal, 0);
            // check if it doesn't go beyond maxSpeed
            if (marioBody.linearVelocity.magnitude < maxSpeed)
                marioBody.AddForce(movement * speed);
        }

        // stop
        if (Input.GetKeyUp("a") || Input.GetKeyUp("d"))
        {
            // stop
            marioBody.linearVelocity = Vector2.zero;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            GameOver();

            Time.timeScale = 0.0f;
        }
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
            eachChild.transform.position = eachChild.GetComponent<EnemyMovement>().startPosition;
        }

        // reset score
        jumpOverGoomba.score = 0;

        gameOverUI.SetActive(false);
        gamePlayUI.SetActive(true);
    }
}