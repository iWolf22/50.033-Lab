using UnityEngine;

public class FireballController : MonoBehaviour
{
    float age;
    void Update()
    {
        age += Time.deltaTime;
        if (age >= 2.5f) Clear();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<EnemyMovement>(out var enemy)) return;
        enemy.Stomp();
        Clear();
    }
    public void Clear()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
