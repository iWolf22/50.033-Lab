using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BlockBounce : MonoBehaviour, IPowerupController
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
    [SerializeField] protected Rigidbody2D springBody;
    [SerializeField] BasePowerup powerup;
    public IPowerup Powerup => powerup;
    [SerializeField] AnimationEventIntTool coinReward;
    Vector2 startPosition;
    Sprite initialSprite;
    bool busy;
    protected bool spent;
    protected virtual bool CanBreak(PlayerMovement player) => false;

    public virtual void Disable() => spent = true;

    void Awake()
    {
        audioSource = FindFirstObjectByType<SceneAudio>().blocks;
        startPosition = transform.position;
        initialSprite = visual.sprite;
        if (coin != null) coin.gameObject.SetActive(false);
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

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (busy || (isQuestionBlock && spent) ||
            !collision.gameObject.TryGetComponent<PlayerMovement>(out var player) || !player.alive) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y < 0.5f) continue;
            if (CanBreak(player))
            {
                spent = true;
                visual.enabled = false;
                GetComponent<BoxCollider2D>().enabled = false;
                audioSource.PlayOneShot(bumpSound);
                return;
            }
            StartCoroutine(Bounce());
            break;
        }
    }

    public void GameRestart()
    {
        StopAllCoroutines();
        busy = spent = false;
        audioSource.Stop();
        visual.enabled = true;
        GetComponent<BoxCollider2D>().enabled = true;
        if (powerup != null) powerup.ResetPowerup();
        if (coin != null) coin.transform.localPosition = Vector3.zero;
        if (coin != null) coin.gameObject.SetActive(false);
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
            powerup.SpawnPowerup();
            yield return new WaitForSeconds(0.5f);
            Disable();
        }
        else yield return new WaitForSeconds(0.35f);
        busy = false;
    }
}
