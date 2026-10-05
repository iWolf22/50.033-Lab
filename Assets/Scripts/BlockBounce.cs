using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class BlockBounce : MonoBehaviour
{
    private Animator anim;
    private bool isBouncing;

    [Header("Coin (tick on for brown-brick-coin only)")]
    public bool spawnCoin = false;
    public Sprite coinSprite;
    public float coinPopHeight = 1.5f;
    public float coinPopTime = 0.5f;
    private bool coinSpent = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
        // Prevent default-state auto-play at game start: park on last frame (y=0) and pause
        anim.Play("Block Bounce", -1, 1f);
        anim.speed = 0;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (isBouncing) return;
        // Only react to Mario (Mario is Untagged, so check component)
        if (col.gameObject.GetComponent<PlayerMovement>() == null) return;
        // Head-hit: Mario must be below the brick
        if (col.transform.position.y > transform.position.y) return;

        isBouncing = true;
        anim.speed = 1;
        anim.Play("Block Bounce", -1, 0f);
        Invoke(nameof(ResetBounce), 0.35f);

        // Coin only pops on the first head-hit
        if (spawnCoin && !coinSpent)
        {
            coinSpent = true;
            StartCoroutine(CoinPop());
        }
    }

    void ResetBounce()
    {
        isBouncing = false;
        // Park back at rest pose so we don't sit playing
        anim.Play("Block Bounce", -1, 1f);
        anim.speed = 0;
    }

    private IEnumerator CoinPop()
    {
        if (coinSprite == null)
        {
            Debug.LogWarning("BlockBounce: coinSprite not assigned.", this);
            yield break;
        }
        // Spawn at brick center, render behind brick
        GameObject coin = new GameObject("CoinPop");
        var sr = coin.AddComponent<SpriteRenderer>();
        sr.sprite = coinSprite;
        int brickOrder = 0;
        var brickSr = GetComponent<SpriteRenderer>();
        if (brickSr != null) brickOrder = brickSr.sortingOrder;
        sr.sortingOrder = brickOrder - 1;

        Vector3 start = transform.position;
        // Account for non-centered sprite pivot (your coin is pivot 0,0)
        Vector3 pivotOffset = (Vector3)coinSprite.bounds.center;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, coinPopTime);
            float k = Mathf.Clamp01(t);
            // 0 -> peak -> 0 (up then back down)
            float yOff = coinPopHeight * (1f - (2f * k - 1f) * (2f * k - 1f));
            coin.transform.position = start + Vector3.up * yOff - pivotOffset;
            yield return null;
        }

        Destroy(coin);
    }
}
