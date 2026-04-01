using UnityEngine;

/// Low-angle camera with deadzone + SmoothDamp follow.
/// Stays near ground level, looks into the field — matches the
/// "ground-level perspective" reference view.
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Drag the GameManager (SnakeGame) here")]
    public SnakeGame snakeGame;

    [Header("Offset from XZ anchor (world units)")]
    public Vector3 offset = new Vector3(0f, 14f, -14f);

    [Header("Look-ahead offset applied to LookAt target")]
    public Vector3 lookAhead = new Vector3(0f, 0f, 0f);

    [Header("Deadzone – grid units before camera slides")]
    public float deadzone = 3f;

    [Header("SmoothDamp time")]
    public float smoothTime = 0.5f;

    private Vector3 _anchor;
    private Vector3 _vel;

    void Start()
    {
        if (snakeGame == null) return;
        _anchor = Flat(snakeGame.HeadWorldPos);
        transform.position = _anchor + offset;
        transform.LookAt(_anchor + lookAhead);
    }

    void LateUpdate()
    {
        if (snakeGame == null) return;

        Vector3 head = Flat(snakeGame.HeadWorldPos);
        float   dist = Vector3.Distance(head, _anchor);

        if (dist > deadzone)
        {
            Vector3 dir     = (head - _anchor).normalized;
            Vector3 desired = head - dir * deadzone;
            _anchor = Vector3.SmoothDamp(_anchor, desired, ref _vel, smoothTime);
        }
        else
        {
            _vel = Vector3.Lerp(_vel, Vector3.zero, Time.deltaTime * 10f);
        }
        
        transform.position = _anchor + offset;
        transform.LookAt(_anchor + lookAhead);
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
