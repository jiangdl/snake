using UnityEngine;

public class FoodFloat : MonoBehaviour
{
    public float amplitude = 0.15f;
    public float speed     = 2.2f;

    private float _startY;

    void Awake()
    {
        var lt       = gameObject.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.color     = new Color(1f, 0.10f, 0.04f);
        lt.intensity = 3f;
        lt.range     = 5f;
    }

    void Start() { _startY = transform.position.y; }

    void Update()
    {
        transform.position = new Vector3(
            transform.position.x,
            _startY + Mathf.Sin(Time.time * speed) * amplitude,
            transform.position.z);
    }
}

