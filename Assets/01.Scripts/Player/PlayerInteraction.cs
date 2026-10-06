using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;

    [Header("Throwable")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float throwForce = 8f;

    private PlayerInput playerInput;

    private InputAction interactAction;
    private InputAction throwAction;

    private ThrowableObject heldObject;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        interactAction = playerInput.actions["Interact"];
        throwAction = playerInput.actions["Throw"];
    }

    private void Update()
    {
        if (interactAction.WasPressedThisFrame())
        {
            Interact();
        }

        if (throwAction.WasPressedThisFrame() && heldObject != null)
        {
            Throw();
        }
    }

    private void Interact()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;

        if (!Physics.Raycast(origin, dir, out RaycastHit hit, interactDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable != null)
        {
            interactable.Interact(this);
        }
    }

    public void PickUp(ThrowableObject target)
    {
        if (target == null || heldObject != null)
        {
            return;
        }

        heldObject = target;
        heldObject.PickUp(holdPoint);
    }

    private void Throw()
    {
        heldObject.Throw(playerCamera.transform.forward, throwForce);
        heldObject = null;
    }
}