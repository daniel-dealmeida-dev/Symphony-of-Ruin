using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerBinder : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    private void Awake()
    {
        GameServices.EnsureInstance();
        if (audioMixer != null)
        {
            GameServices.Instance.Audio.SetAudioMixer(audioMixer);
        }
    }
}
