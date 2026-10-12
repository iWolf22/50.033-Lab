using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BlockBounce : MonoBehaviour
{
    public bool spawnCoin;
    public bool isQuestionBlock;
    public Sprite flashSprite;
    public Sprite solidSprite;
    [SerializeField] SpriteRenderer visual;
    [SerializeField] SpriteRenderer coin;
    [SerializeField] Animator bounceAnimator;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip coinSound;
    [SerializeField] AudioClip bumpSound;
    [SerializeField] Rigidbody2D springBody;
    [SerializeField] AnimationEventIntTool coinReward;
    Vector2 startPosition;
    Sprite initialSprite;
    bool busy;
    bool spent;

    void OnEnable()
    {
        GameManager.instance.gameRestart.AddListener(GameRestart);
        coinReward.useInt.AddListener(GameManager.instance.IncreaseScore);
    }

    void OnDisable()
    {
        if (GameManager.instance == null) return;
        GameManager.instance.gameRestart.RemoveListener(GameRestart);
        coinReward.useInt.RemoveListener(GameManager.instance.IncreaseScore);
    }

    void Awake()
    {
        startPosition = transform.position;
        initialSprite = visual.sprite;
        coin.gameObject.SetActive(false);
        if (bounceAnimator != null)
        {
            bounceAnimator.Play("Block Bounce", 0, 1);
            bounceAnimator.Update(0);
            bounceAnimator.speed = 0;
        }
    }

    void Update()
    {
        if (isQuestionBlock && !spent)
            visual.sprite = Mathf.FloorToInt(Time.time * 4) % 2 == 0 ? initialSprite : flashSprite;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (busy || (isQuestionBlock && spent) ||
            !collision.gameObject.TryGetComponent<PlayerMovement>(out var player) || !player.alive) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y < 0.5f) continue;
            StartCoroutine(Bounce());
            break;
        }
    }

    public void GameRestart()
    {
        StopAllCoroutines();
        busy = spent = false;
        audioSource.Stop();
        coin.transform.localPosition = Vector3.zero;
        coin.gameObject.SetActive(false);
        visual.sprite = initialSprite;
        if (bounceAnimator != null)
        {
            bounceAnimator.Play("Block Bounce", 0, 1);
            bounceAnimator.Update(0); bounceAnimator.speed = 0;
        }
        visual.transform.localPosition = Vector3.zero;
        if (springBody != null)
        {
            springBody.bodyType = RigidbodyType2D.Dynamic;
            springBody.position = startPosition;
            transform.position = startPosition;
            springBody.linearVelocity = Vector2.zero;
        }
    }

    IEnumerator Bounce()
    {
        busy = true;
        bool reward = (spawnCoin || isQuestionBlock) && !spent;
        if (reward) spent = true;
        if (isQuestionBlock) visual.sprite = solidSprite;
        audioSource.PlayOneShot(reward ? coinSound : bumpSound);
        if (bounceAnimator != null)
        {
            bounceAnimator.speed = 1;
            bounceAnimator.Play("Block Bounce", 0, 0);
        }
        if (springBody != null) springBody.AddForce(Vector2.up * 4, ForceMode2D.Impulse);
        if (reward)
        {
            coin.gameObject.SetActive(true);
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                coin.transform.localPosition = Vector3.up * (1.8f * Mathf.Sin(t * Mathf.PI / 0.5f));
                yield return null;
            }
            coin.transform.localPosition = Vector3.zero;
            coin.gameObject.SetActive(false);
            coinReward.TriggerIntEvent();
        }
        else yield return new WaitForSeconds(0.35f);
        if (springBody != null && isQuestionBlock) springBody.bodyType = RigidbodyType2D.Static;
        busy = false;
    }
}
