using UnityEngine;
using UnityEngine.AI;

public class GuardAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Footstep Audio")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip[] walkFootsteps;
    [SerializeField] private AudioClip[] runFootsteps;

    [Header("Footstep Interval")]
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.35f;

    [Header("Footstep Volume")]
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.45f;
    [SerializeField, Range(0f, 1f)] private float runVolume = 0.7f;

    private NavMeshAgent agent;
    private GuardController guard;

    private float stepTimer;
    private int stepIndex;
    private bool wasRunning;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        guard = GetComponent<GuardController>();
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        Vector3 velocity = agent.velocity;
        velocity.y = 0f;

        float speed = velocity.magnitude;

        if (animator != null)
        {
            animator.SetFloat(MoveSpeedHash, speed);
        }

        UpdateFootsteps(speed);
    }

    private void UpdateFootsteps(float speed)
    {
        if (footstepAudioSource == null || guard == null || guard.Data == null || speed < 0.1f || Time.timeScale <= 0f)
        {
            stepTimer = 0f;
            return;
        }

        bool isRunning = guard.IsChasing;

        if (wasRunning != isRunning)
        {
            wasRunning = isRunning;
            stepTimer = 0f;
        }

        // 실제 이동 속도에 따라 발걸음 간격 보정
        float baseSpeed = isRunning ? guard.Data.chaseSpeed : guard.Data.patrolSpeed;
        float speedMultiplier = Mathf.Clamp(speed / Mathf.Max(0.1f, baseSpeed), 0.7f, 1.5f);

        float interval = isRunning ? runStepInterval : walkStepInterval;
        stepTimer += Time.deltaTime * speedMultiplier;

        if (stepTimer < interval)
        {
            return;
        }

        stepTimer = 0f;

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

        footstepAudioSource.PlayOneShot(clip, isRunning ? runVolume : walkVolume);
    }
}