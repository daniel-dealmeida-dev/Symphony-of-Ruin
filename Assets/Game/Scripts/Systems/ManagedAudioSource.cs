using UnityEngine;

public enum AudioBus
{
    Music,
    Sfx,
    Ui
}

[DisallowMultipleComponent]
public class ManagedAudioSource : MonoBehaviour
{
    [SerializeField] private AudioBus bus = AudioBus.Sfx;

    public AudioBus Bus
    {
        get { return bus; }
        set { bus = value; }
    }

    private AudioSource cachedSource;
    private float baseVolume = 1f;

    private void Awake()
    {
        cachedSource = GetComponent<AudioSource>();
        if (cachedSource != null)
        {
            baseVolume = cachedSource.volume;
        }
    }

    private void OnEnable()
    {
        GameServices.EnsureInstance();
        if (GameServices.HasInstance)
        {
            GameServices.Instance.Audio.Register(this);
        }
    }

    private void OnDisable()
    {
        if (GameServices.HasInstance)
        {
            GameServices.Instance.Audio.Unregister(this);
        }
    }

    public void ApplyVolume(float master, float channelVolume)
    {
        if (cachedSource == null)
        {
            cachedSource = GetComponent<AudioSource>();
        }

        if (cachedSource != null)
        {
            cachedSource.volume = baseVolume * master * channelVolume;
        }
    }
}
