using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public Transform player;
    public Transform endLimit;
    public Transform leftBoundary;
    float startX;
    float offset;
    Camera view;

    void OnEnable() => GameManager.instance.gameRestart.AddListener(GameRestart);

    void OnDisable()
    {
        if (GameManager.instance != null)
            GameManager.instance.gameRestart.RemoveListener(GameRestart);
    }

    void Start()
    {
        view = GetComponent<Camera>();
        startX = transform.position.x;
        offset = startX - player.position.x;
        if (leftBoundary != null)
            leftBoundary.position = new Vector3(startX - view.orthographicSize * view.aspect - 0.25f, 1, 0);
    }

    public void GameRestart() => transform.position = new Vector3(startX, transform.position.y, transform.position.z);

    void LateUpdate()
    {
        float endX = Mathf.Max(startX, endLimit.position.x - view.orthographicSize * view.aspect);
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(player.position.x + offset, startX, endX);
        transform.position = position;
    }
}
