using UnityEngine;

public class TailWiggleUI : MonoBehaviour
{
    public float angle = 15f;
    public float speed = 5f;

    private Quaternion startRotation;

    void Awake()
    {
        startRotation = transform.localRotation;
    }

    void LateUpdate() 
    {
        float z = Mathf.Sin(Time.unscaledTime * speed) * angle;
        transform.localRotation = startRotation * Quaternion.Euler(0f, 0f, z);
    }
}
