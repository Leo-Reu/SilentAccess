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

    private CharacterController controller;
    private PlayerController playerController;

    private float stepTimer;

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
    }
}