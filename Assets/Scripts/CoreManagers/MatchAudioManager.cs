using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MatchAudioManager : MonoBehaviour
{
    public static MatchAudioManager Instance;

    [Header("Match Audio Clips")]
    public AudioClip tickSound;
    public AudioClip startSound;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
        }
        else 
        { 
            Destroy(gameObject); 
            return;
        }

        _audioSource = GetComponent<AudioSource>();
        
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
    }

    public void PlayTickSound()
    {
        if (tickSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(tickSound);
        }
    }

    public void PlayStartSound()
    {
        if (startSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(startSound);
        }
    }
}