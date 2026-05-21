using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    public AudioSource source;
    public AudioClip attackSound1;   // Z — Ataque 1
    public AudioClip attackSound2;   // X — Ataque 2
    public AudioClip attackSoundStrong; // V — Ataque forte

    public void PlayAttackSound(int indice)
    {
        AudioClip clip = indice switch
        {
            0 => attackSound1,
            1 => attackSound2,
            3 => attackSoundStrong,
            _ => attackSound1
        };

        if (source != null && clip != null)
            source.PlayOneShot(clip);
    }
}