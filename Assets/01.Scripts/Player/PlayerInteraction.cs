using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;

    [Header("Throwable")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float throwForce = 8f;

    [Header("Interaction UI")]
    [SerializeField] private GameObject interactionPromptPanel;
    [SerializeField] private TextMeshProUGUI interactionPromptText;

    private PlayerInput playerInput;
    private PlayerController playerController;
    private PlayerLook playerLook;
    private PlayerFootstepNoise footstepNoise;

    private bool canInteract = true;

    private InputAction interactAction;
    private InputAction throwAction;

    private ThrowableObject heldObject;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerController = GetComponent<PlayerController>();
        playerLook = GetComponent<PlayerLook>();
        footstepNoise = GetComponent<PlayerFootstepNoise>();

        interactAction = playerInput.actions["Interact"];
        throwAction = playerInput.actions["Throw"];

        if (interactionPromptPanel != null)
        {
            interactionPromptPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!canInteract)
        {
            SetInteractionPromptVisible(false);
            return;
        }

        UpdateInteractionPrompt();

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
        if (TryGetInteractable(out IInteractable interactable, out _))
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

    public void SetControlEnabled(bool value)
    {
        canInteract = value;

        if (playerController != null)
        {
            playerController.enabled = value;
        }

        if (playerLook != null)
        {
            playerLook.enabled = value;
        }

        if (footstepNoise != null)
        {
            footstepNoise.enabled = value;
        }

        if (!value)
        {
            SetInteractionPromptVisible(false);
        }

        Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !value;
    }


    private bool TryGetInteractable(out IInteractable interactable, out InteractionPromptData promptData)
    {
        interactable = null;
        promptData = null;

        if (playerCamera == null)
        {
            return false;
        }

        Vector3 origin = playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;

        if (!Physics.Raycast(origin, dir, out RaycastHit hit, interactDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null)
        {
            return false;
        }

        promptData = hit.collider.GetComponentInParent<InteractionPromptData>();
        return true;
    }

    private void UpdateInteractionPrompt()
    {
        if (interactionPromptPanel == null || interactionPromptText == null)
        {
            return;
        }

        if (!TryGetInteractable(out _, out InteractionPromptData promptData) || promptData == null || string.IsNullOrWhiteSpace(promptData.ActionText))
        {
            SetInteractionPromptVisible(false);
            return;
        }

        string message = promptData.ActionText;

        if (interactionPromptText.text != message)
        {
            interactionPromptText.text = message;
        }

        SetInteractionPromptVisible(true);
    }

    private void SetInteractionPromptVisible(bool visible)
    {
        if (interactionPromptPanel != null && interactionPromptPanel.activeSelf != visible)
        {
            interactionPromptPanel.SetActive(visible);
        }
    }

}