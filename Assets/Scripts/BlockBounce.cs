using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class BlockBounce : MonoBehaviour
{
    private Animator anim;
    private bool isBouncing;
    private SpriteRenderer sr;

    [Header("Coin (tick on for brown-brick-coin only)")]
    public bool spawnCoin = false;
    public Sprite coinSprite;
    public float coinPopHeight = 1.5f;
    public float coinPopTime = 0.5f;
    private bool coinSpent = false;

    [Header("Question block (auto-detected by name, no setup needed)")]
    public bool isQuestionBlock = false;
    public Sprite questionSprite;
    public Sprite flashSprite;
    public Sprite solidSprite;
    public float flashRate = 0.25f;
    private bool spent = false;
    private Coroutine flashRoutine;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();

        // Park the Animator bounce on its rest frame so nothing auto-plays.
        // Guarded: question blocks bootstrapped at runtime may have no controller.
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            try
            {
                anim.Play("Block Bounce", -1, 1f);
                anim.speed = 0;
            }
            catch { }
        }

        // No editor setup needed: anything named *question* behaves as a question block.
        if (!isQuestionBlock && gameObject.name.ToLower().Contains("question"))
            isQuestionBlock = true;

        if (isQuestionBlock)
        {
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            if (questionSprite == null)
                questionSprite = sr.sprite != null ? sr.sprite : FindSpriteByName("question-mark");
            if (flashSprite == null)
                flashSprite = FindSpriteByName("question-mark-flash");
            if (solidSprite == null)
            {
                solidSprite = FindSpriteByName("question-mark-solid");
                if (solidSprite == null) solidSprite = FindSpriteByName("brown-brick");
            }
            if (coinSprite == null)
                coinSprite = FindSpriteByName("coin");
            if (sr.sprite == null && questionSprite != null)
                sr.sprite = questionSprite;
        }
        else if (spawnCoin && coinSprite == null)
        {
            coinSprite = FindSpriteByName("coin");
        }
    }

    void Start()
    {
        if (isQuestionBlock && !spent && sr != null)
        {
            if (sr.sprite == null && questionSprite != null)
                sr.sprite = questionSprite;
            flashRoutine = StartCoroutine(FlashLoop());
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (isBouncing) return;
        // Only react to Mario (Mario is Untagged, so check component)
        if (col.gameObject.GetComponent<PlayerMovement>() == null) return;
        // Head-hit: Mario must be below the brick
        if (col.transform.position.y > transform.position.y) return;

        if (isQuestionBlock)
        {
            if (spent) return; // solid now: no bounce, no coin
            spent = true;
            coinSpent = true;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            if (sr != null)
            {
                sr.color = Color.white;
                if (solidSprite != null) sr.sprite = solidSprite;
            }
            Bounce();
            StartCoroutine(CoinPop());
            return;
        }

        Bounce();

        // Coin only pops on the first head-hit
        if (spawnCoin && !coinSpent)
        {
            coinSpent = true;
            StartCoroutine(CoinPop());
        }
    }

    void Bounce()
    {
        isBouncing = true;
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            try
            {
                anim.speed = 1;
                anim.Play("Block Bounce", -1, 0f);
                Invoke(nameof(ResetBounce), 0.35f);
                return;
            }
            catch { }
        }
        // No Animator/controller (e.g. runtime-bootstrapped question block):
        // same 0 -> 0.4 -> 0 hop as block-bounce.anim, done in code.
        StartCoroutine(BounceTransform());
    }

    void ResetBounce()
    {
        isBouncing = false;
        // Park back at rest pose so we don't sit playing
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            try
            {
                anim.Play("Block Bounce", -1, 1f);
                anim.speed = 0;
            }
            catch { }
        }
    }

    private IEnumerator BounceTransform()
    {
        Vector3 basePos = transform.position;
        float dur = 0.33f, height = 0.4f, t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, dur);
            float k = Mathf.Clamp01(t);
            float yOff = height * (1f - (2f * k - 1f) * (2f * k - 1f));
            transform.position = basePos + Vector3.up * yOff;
            yield return null;
        }
        transform.position = basePos;
        isBouncing = false;
    }

    private IEnumerator FlashLoop()
    {
        bool showFlash = false;
        while (!spent)
        {
            if (sr != null)
            {
                if (questionSprite != null && flashSprite != null)
                    sr.sprite = showFlash ? flashSprite : questionSprite;
                else
                    sr.color = showFlash ? new Color(1f, 0.75f, 0.3f, 1f) : Color.white;
            }
            showFlash = !showFlash;
            yield return new WaitForSeconds(flashRate);
        }
    }

    private IEnumerator CoinPop()
    {
        if (coinSprite == null)
            coinSprite = FindSpriteByName("coin");
        if (coinSprite == null)
        {
            Debug.LogWarning("BlockBounce: coin sprite not found.", this);
            yield break;
        }
        // Spawn at brick center, render behind brick
        GameObject coin = new GameObject("CoinPop");
        var csr = coin.AddComponent<SpriteRenderer>();
        csr.sprite = coinSprite;
        int brickOrder = 0;
        var brickSr = GetComponent<SpriteRenderer>();
        if (brickSr != null) brickOrder = brickSr.sortingOrder;
        csr.sortingOrder = brickOrder - 1;

        Vector3 start = transform.position;
        // Harmless when pivot is centered; keeps old pivot-0,0 coins aligned
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

    private static Sprite FindSpriteByName(string spriteName)
    {
        var all = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (var s in all)
            if (s != null && s.name == spriteName) return s;
        return null;
    }

    // No editor changes needed: the scene's question-mark instance lost its
    // source prefab (stripped: Transform only + Animator), so give it a
    // SpriteRenderer, collider and this script at runtime if still missing.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSetupQuestionBlocks()
    {
        var transforms = Object.FindObjectsOfType<Transform>(true);
        foreach (var tr in transforms)
        {
            if (tr == null || tr.name == "CoinPop") continue;
            if (!tr.name.ToLower().Contains("question")) continue;
            var go = tr.gameObject;
            if (go.GetComponent<SpriteRenderer>() == null)
                go.AddComponent<SpriteRenderer>();
            var col = go.GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = go.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
            }
            if (go.GetComponent<BlockBounce>() == null)
                go.AddComponent<BlockBounce>();
        }
    }
}
