using UnityEngine;

public class ThrowableObject : MonoBehaviour, IInteractable
{
    [Header("Noise")]
    [SerializeField] private float noiseRadius = 15f;
    [SerializeField] private float minImpactSpeed = 2f;

    private Rigidbody rb;
    private Collider col;

    private bool canMakeNoise;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Interact(PlayerInteraction player)
    {
        player.PickUp(this);
    }

    public void PickUp(Transform holdPoint)
    {
        canMakeNoise = false;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        col.enabled = false;

        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Throw(Vector3 dir, float force)
    {
        transform.SetParent(null);

        col.enabled = true;
        rb.isKinematic = false;

        canMakeNoise = true;

        // 투척 효과음 재생
        SoundManager.Instance?.PlaySFXAtPosition(SFXType.Throw, transform.position);

        rb.AddForce(dir * force, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!canMakeNoise || collision.relativeVelocity.magnitude < minImpactSpeed)
        {
            return;
        }

        canMakeNoise = false;

        NoiseSystem.GenerateNoise(transform.position, noiseRadius, NoiseType.ThrownObject);

        // 충돌 위치에서 3D 효과음 재생
        SoundManager.Instance?.PlaySFXAtPosition(SFXType.ThrowImpact, transform.position);
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, noiseRadius);
    }
}