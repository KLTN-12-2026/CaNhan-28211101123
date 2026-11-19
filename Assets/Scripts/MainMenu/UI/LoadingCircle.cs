using UnityEngine;
using UnityEngine.UI;

public class LoadingCircle : MonoBehaviour
{
    public float rotateSpeed = 200f;
    public Image target;
    public Color glowColor = Color.cyan;
    public float pulseSpeed = 2f;

    void Update()
    {
        transform.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
        float glow = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        target.color = Color.Lerp(Color.white, glowColor, glow);
    }
}
