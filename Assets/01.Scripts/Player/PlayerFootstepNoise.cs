using UnityEngine;

public class PlayerFootstepNoise : MonoBehaviour
{
    [Header("Noise Radius")]
    [SerializeField] private float walkNoiseRadius = 6f;
    [SerializeField] private float runNoiseRadius = 12f;
    [SerializeField] private float crouchNoiseRadius = 2f;

    [Header("Step Interval")]
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.35f;
    [SerializeField] private float crouchStepInterval = 0.8f;

    [Header("Footstep Audio")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip[] walkFootsteps;
    [SerializeField] private AudioClip[] runFootsteps;

    [Header("Footstep Volume")]
    [SerializeField, Range(0f, 1f)] private float crouchVolume = 0.15f;
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float runVolume = 0.6f;

    private CharacterController controller;
    private PlayerController playerController;

    private float stepTimer;
    private int stepIndex;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
    }

    private void LateUpdate()
    {
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;

        if (!controller.isGrounded || velocity.sqrMagnitude < 0.01f)
        {
            stepTimer = 0f;
            return;
        }

        float noiseRadius = walkNoiseRadius;
        float stepInterval = walkStepInterval;

        if (playerController.IsCrouch)
        {
            noiseRadius = crouchNoiseRadius;
            stepInterval = crouchStepInterval;
        }
        else if (playerController.IsRunning)
        {
            noiseRadius = runNoiseRadius;
            stepInterval = runStepInterval;
        }

        stepTimer += Time.deltaTime;

        if (stepTimer < stepInterval)
        {
            return;
        }

        stepTimer = 0f;

        NoiseSystem.GenerateNoise(transform.position, noiseRadius, NoiseType.Footstep);
        PlayFootstep();
    }
    private void PlayFootstep()
    {
        if (footstepAudioSource == null)
        {
            return;
        }

        bool isRunning = playerController.IsRunning;
        AudioClip[] clips = isRunning && runFootsteps != null && runFootsteps.Length > 0 ? runFootsteps : walkFootsteps;

        if (clips == null || clips.Length == 0)
        {
            return;
        }

        AudioClip clip = clips[stepIndex % clips.Length];
        stepIndex++;

        if (clip == null)
        {
            return;
        }

        float volume = playerController.IsCrouch ? crouchVolume : isRunning ? runVolume : walkVolume;
        footstepAudioSource.PlayOneShot(clip, volume);
    }

    private void OnDisable()
    {
        stepTimer = 0f;
    }

}