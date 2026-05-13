using UnityEngine;

/// <summary>
/// Controla música (loop) e efeitos sonoros com volumes básicos.
/// Persiste entre cenas para manter continuidade de áudio no fluxo menu → jogo.
/// </summary>
[DefaultExecutionOrder(-400)]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Volumes (0–1)")]
    [Range(0f, 1f)] public float musicVolume = 0.45f;
    [Range(0f, 1f)] public float sfxVolume = 0.7f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private AudioClip titleClip;
    private AudioClip gameplayClip;
    private AudioClip gameOverClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        titleClip = ProceduralToneLibrary.TitleLoop();
        gameplayClip = ProceduralToneLibrary.GameplayLoop();
        gameOverClip = ProceduralToneLibrary.GameOverSting();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayTitleMusic()
    {
        PlayMusicClip(titleClip, musicVolume * 0.85f);
    }

    public void PlayGameplayMusic()
    {
        PlayMusicClip(gameplayClip, musicVolume);
    }

    /// <summary>Música distinta (ou sting) ao entrar em game over.</summary>
    public void PlayGameOverMusic()
    {
        musicSource.loop = false;
        musicSource.clip = gameOverClip;
        musicSource.volume = musicVolume * 0.9f;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    private void PlayMusicClip(AudioClip clip, float volume)
    {
        musicSource.loop = true;
        if (musicSource.isPlaying && musicSource.clip == clip)
        {
            musicSource.volume = volume;
            return;
        }

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.Play();
    }

    public void PlayJump()
    {
        PlayOneShot(ProceduralToneLibrary.JumpBlip());
    }

    public void PlayDeathOrHit()
    {
        PlayOneShot(ProceduralToneLibrary.HitDeath());
    }

    public void PlayCollect()
    {
        PlayOneShot(ProceduralToneLibrary.CoinPickup());
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
