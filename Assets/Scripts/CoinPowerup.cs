using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AnimationEventIntTool))]
public class CoinPowerup : BasePowerup
{
    public override PowerupType powerupType => PowerupType.Coin;

    public override void SpawnPowerup()
    {
        if (hasSpawned) return;
        gameObject.SetActive(true);
        hasSpawned = true;
        StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        Vector3 origin = transform.localPosition;
        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            transform.localPosition = origin + Vector3.up * (1.8f * Mathf.Sin(t * Mathf.PI / 0.5f));
            yield return null;
        }
        ApplyPowerup(GameManager.instance);
        transform.localPosition = origin;
        DestroyPowerup();
    }

    public override void ApplyPowerup(MonoBehaviour target)
    {
        if (consumed || target is not GameManager) return;
        consumed = true;
        GetComponent<AnimationEventIntTool>().TriggerIntEvent();
    }
}
