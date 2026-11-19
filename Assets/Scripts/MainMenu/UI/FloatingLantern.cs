using UnityEngine;

public class FloatingLantern : MonoBehaviour
{
    [Header("Floating Settings")]
    public float amplitude = 20f;   // Độ cao bay lên xuống (UI: pixel, World: unit)
    public float speed = 1f;        // Tốc độ bay

    private Vector3 startPos;

    void Awake()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.unscaledTime * speed) * amplitude;
        transform.localPosition = startPos + new Vector3(0f, y, 0f);
    }
}
