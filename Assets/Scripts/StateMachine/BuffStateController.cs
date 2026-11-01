using UnityEngine;

public class BuffStateController : StateController
{
    public bool IsStarActive { get; private set; }
    SpriteRenderer visual;
    AudioSource starMusic;
    AudioSource background;
    void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        var audio = FindFirstObjectByType<SceneAudio>();
        starMusic = audio.starMusic;
        background = audio.GetComponentInChildren<BackgroundMusic>().GetComponent<AudioSource>();
    }
    public void ReceivePowerup(IPowerup powerup)
    {
        if (!GetComponent<PlayerMovement>().alive || powerup.powerupType != PowerupType.StarMan) return;
        // Re-enter to refresh a collected star without changing Mario's form.
        if (IsStarActive) { TransitionToState(startState); }
        PendingPowerup = PowerupType.StarMan;
        currentState.Tick(this);
    }
    public void SetStar(bool active)
    {
        IsStarActive = active;
        visual.color = Color.white;
        if (active) { background.Pause(); starMusic.Play(); }
        else { starMusic.Stop(); background.UnPause(); }
    }
    protected override void Update()
    {
        base.Update();
        if (IsStarActive) visual.color = Color.HSVToRGB(Mathf.Repeat(stateTimeElapsed * 2, 1), 0.65f, 1);
    }
}
