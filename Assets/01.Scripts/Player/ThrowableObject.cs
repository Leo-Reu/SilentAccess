using UnityEngine;

public class ThrowableObject : MonoBehaviour, IInteractable
{
    private Rigidbody rb;
    private Collider col;

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

        rb.AddForce(dir * force, ForceMode.Impulse);
    }
}