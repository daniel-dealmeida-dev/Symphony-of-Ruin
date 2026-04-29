using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyPresentation2D : MonoBehaviour
{
    private Animator animator;
    private AudioSource audioSource;
    private string idleState;
    private string moveState;
    private string attackState;
    private string deathState;
    private bool moving;
    private Coroutine attackRoutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        string lowerName = gameObject.name.ToLowerInvariant();
        bool isWolf = lowerName.Contains("wolf");
        bool isCrow = lowerName.Contains("crow");

        idleState = "Idle";
        moveState = isWolf ? "Run" : isCrow ? "Fly" : "Idle";
        attackState = isWolf ? "Attack" : moveState;
        deathState = "Death";

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.Stop();
        }

        PlayState(idleState);
    }

    public void SetMoving(bool shouldMove)
    {
        if (attackRoutine != null)
        {
            return;
        }

        if (moving == shouldMove)
        {
            UpdateAudioState(shouldMove);
            return;
        }

        moving = shouldMove;
        PlayState(shouldMove ? moveState : idleState);
        UpdateAudioState(shouldMove);
    }

    public void PlayAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
        }

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    public void StopAllPresentation()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        moving = false;
        PlayState(idleState);
        UpdateAudioState(false);
    }

    public void PlayDeath()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        moving = false;
        PlayState(deathState);
        UpdateAudioState(false);
    }

    private IEnumerator AttackRoutine()
    {
        PlayState(attackState);
        UpdateAudioState(false);
        yield return new WaitForSeconds(0.35f);
        attackRoutine = null;
        PlayState(moving ? moveState : idleState);
        UpdateAudioState(moving);
    }

    private void PlayState(string stateName)
    {
        if (animator != null && !string.IsNullOrWhiteSpace(stateName) && animator.runtimeAnimatorController != null)
        {
            int stateHash = Animator.StringToHash(stateName);
            if (animator.HasState(0, stateHash))
            {
                animator.Play(stateHash, 0, 0f);
            }
        }
    }

    private void UpdateAudioState(bool shouldMove)
    {
        if (audioSource == null || audioSource.clip == null || !audioSource.isActiveAndEnabled)
        {
            return;
        }

        if (shouldMove)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
