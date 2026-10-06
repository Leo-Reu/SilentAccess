using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Crouch")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float standHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standCameraY = 1.6f;
    [SerializeField] private float crouchCameraY = 0.9f;
    [SerializeField] private float crouchChangeSpeed = 6f;
    [SerializeField] private LayerMask obstacleLayer;

    private CharacterController controller;
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    private float yVelocity;
    private bool isCrouch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];
        crouchAction = playerInput.actions["Crouch"];
    }

    private void Update()
    {
        Crouch();
        Move();
    }

    private void Move()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;

        float currentSpeed;

        if (isCrouch)
        {
            currentSpeed = crouchSpeed;
        }
        else if (sprintAction.IsPressed())
        {
            currentSpeed = runSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        if (controller.isGrounded && yVelocity < 0f)
        {
            yVelocity = -2f;
        }

        yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * currentSpeed;
        velocity.y = yVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    private void Crouch()
    {
        bool crouchInput = crouchAction.IsPressed();

        if (!crouchInput && !CanStand())
        {
            crouchInput = true;
        }

        isCrouch = crouchInput;

        float targetHeight = isCrouch ? crouchHeight : standHeight;
        float targetCameraY = isCrouch ? crouchCameraY : standCameraY;

        controller.height = Mathf.MoveTowards(controller.height, targetHeight, crouchChangeSpeed * Time.deltaTime);

        Vector3 center = controller.center;
        center.y = controller.height / 2f;
        controller.center = center;

        Vector3 cameraPos = cameraRoot.localPosition;
        cameraPos.y = Mathf.MoveTowards(cameraPos.y, targetCameraY, crouchChangeSpeed * Time.deltaTime);
        cameraRoot.localPosition = cameraPos;
    }

    private bool CanStand()
    {
        float checkRadius = controller.radius * 0.9f;
        float checkDistance = standHeight - crouchHeight;
        Vector3 checkPos = transform.position + Vector3.up * (crouchHeight - checkRadius);

        return !Physics.SphereCast(checkPos, checkRadius, Vector3.up, out _, checkDistance, obstacleLayer, QueryTriggerInteraction.Ignore);
    }
}