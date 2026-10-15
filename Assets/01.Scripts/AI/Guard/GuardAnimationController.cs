using UnityEngine;
using UnityEngine.AI;

public class GuardAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private NavMeshAgent agent;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (animator == null || !agent.isOnNavMesh)
        {
            return;
        }
        animator.SetFloat(MoveSpeedHash, agent.velocity.magnitude);
    }
}
