using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class MarioStateController : StateController
{
    public MarioForm Form { get; private set; }
    float damageProtectionUntil;
    public bool IsProtected => Form == MarioForm.Hurt || Time.time < damageProtectionUntil;
    public void ProtectFromDamage(float duration) => damageProtectionUntil = Time.time + duration;
    PlayerMovement player;
    SpriteRenderer visual;
    BuffStateController buff;
    SceneAudio sceneAudio;
    [SerializeField] State deadState;
    [SerializeField] AudioClip fireSound;
    [SerializeField] BoolVariable facingRight;

    void Awake()
    {
        player = GetComponent<PlayerMovement>();
        visual = GetComponent<SpriteRenderer>();
        buff = GetComponent<BuffStateController>();
        sceneAudio = FindFirstObjectByType<SceneAudio>();
    }
    public void ReceivePowerup(IPowerup powerup)
    {
        if (!player.alive || powerup.powerupType == PowerupType.StarMan) return;
        SetPowerup(powerup.powerupType);
    }
    public void SetPowerup(PowerupType type)
    {
        PendingPowerup = type;
        currentState.Tick(this);
        ClearPowerup();
    }
    public void Kill()
    {
        if (player.alive) TransitionToState(deadState);
    }
    public void TakeDamage()
    {
        if (player.alive && !IsProtected && !buff.IsStarActive) SetPowerup(PowerupType.Damage);
    }
    public void SetForm(MarioForm form, RuntimeAnimatorController animatorController, AudioClip sound)
    {
        Form = form;
        visual.enabled = true;
        player.SetSuperMario(form == MarioForm.Super || form == MarioForm.Fire);
        if (animatorController != null) player.marioAnimator.runtimeAnimatorController = animatorController;
        if (sound != null) sceneAudio.powerups.PlayOneShot(sound);
        if (form == MarioForm.Dead)
        {
            buff.GameRestart();
            player.Die();
        }
    }
    protected override void Update()
    {
        if (!player.alive) return;
        base.Update();
        visual.enabled = !IsProtected || Mathf.FloorToInt(stateTimeElapsed / 0.1f) % 2 == 0;
    }
    public void Fire()
    {
        if (player.alive && Time.timeScale > 0) currentState.Attack(this);
    }
    public void FireProjectile(FireballController prefab)
    {
        if (FindObjectsByType<FireballController>(FindObjectsSortMode.None).Length >= 3) return;
        float direction = facingRight.Value ? 1 : -1;
        var shot = Instantiate(prefab, transform.position + new Vector3(direction * 0.7f, 0.25f), Quaternion.identity);
        shot.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(direction * 9, 4);
        sceneAudio.powerups.PlayOneShot(fireSound);
    }
    public override void GameRestart()
    {
        damageProtectionUntil = 0;
        foreach (var shot in FindObjectsByType<FireballController>(FindObjectsSortMode.None)) shot.Clear();
        base.GameRestart();
    }
}
