using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BlockBounce : MonoBehaviour
{
    private Animator anim;
    private bool isBouncing;

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
    }

    void ResetBounce()
    {
        isBouncing = false;
        // Park back at rest pose so we don't sit playing
        anim.Play("Block Bounce", -1, 1f);
        anim.speed = 0;
    }
}
