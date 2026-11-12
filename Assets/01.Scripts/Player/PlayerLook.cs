using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float gamepadSensitivity = 150f;

    private PlayerInput playerInput;
    private InputAction lookAction;

    private float xRotation;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        lookAction = playerInput.actions["Look"];

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Look();
    }

    private void Look()
    {
        Vector2 input = lookAction.ReadValue<Vector2>();

        float sensitivity = playerInput.currentControlScheme == "Gamepad" ? gamepadSensitivity * Time.deltaTime : mouseSensitivity;

        float mouseX = input.x * sensitivity;
        float mouseY = input.y * sensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}