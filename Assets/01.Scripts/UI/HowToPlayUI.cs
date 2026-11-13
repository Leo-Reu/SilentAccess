using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HowToPlayUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private Button keyboardBtn;
    [SerializeField] private Button gamepadBtn;

    private InputActionAsset currentActions;
    private InputActionAsset previewActions;
    private bool showGamepad;

    private void Awake()
    {
        if (inputActions != null)
        {
            previewActions = Instantiate(inputActions);
        }

        if (keyboardBtn != null)
        {
            keyboardBtn.onClick.AddListener(ShowKeyboard);
        }

        if (gamepadBtn != null)
        {
            gamepadBtn.onClick.AddListener(ShowGamepad);
        }
    }

    private void OnEnable()
    {
        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();

        currentActions = playerInput != null ? playerInput.actions : previewActions;

        if (currentActions != null)
        {
            currentActions.RemoveAllBindingOverrides();
            InputBindingSave.Load(currentActions);
        }

        if (playerInput != null)
        {
            showGamepad = playerInput.currentControlScheme == "Gamepad";
        }
        else
        {
            showGamepad = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        }

        UpdateKeyText();
    }

    public void ShowKeyboard()
    {
        showGamepad = false;
        UpdateKeyText();
    }

    public void ShowGamepad()
    {
        showGamepad = true;
        UpdateKeyText();
    }

    public void SelectCurrentButton()
    {
        Button button = showGamepad ? gamepadBtn : keyboardBtn;

        if (button == null || EventSystem.current == null || !button.IsActive() || !button.IsInteractable())
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void UpdateKeyText()
    {
        if (keyText == null)
        {
            return;
        }

        if (showGamepad)
        {
            string sprint = GetBindingText("Sprint", "RT", true);
            string crouch = GetBindingText("Crouch", "LT", true);
            string interact = GetBindingText("Interact", "LB", true);
            string throwKey = GetBindingText("Throw", "RB", true);

            keyText.text = $"왼쪽 스틱\n오른쪽 스틱\n{sprint}\n{crouch}\n{interact}\n{throwKey}";
            return;
        }

        string move = GetMoveBindingText();
        string keyboardSprint = GetBindingText("Sprint", "Left Shift", false);
        string keyboardCrouch = GetBindingText("Crouch", "Left Ctrl", false);
        string keyboardInteract = GetBindingText("Interact", "E", false);
        string keyboardThrow = GetBindingText("Throw", "LMB", false);

        keyText.text = $"{move}\n마우스 이동\n{keyboardSprint}\n{keyboardCrouch}\n{keyboardInteract}\n{keyboardThrow}";
    }

    private string GetBindingText(string actionName, string fallback, bool gamepad)
    {
        if (currentActions == null)
        {
            return fallback;
        }

        InputAction action = currentActions.FindAction($"Player/{actionName}", false);

        if (action == null)
        {
            return fallback;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (binding.isComposite || binding.isPartOfComposite || string.IsNullOrEmpty(binding.path))
            {
                continue;
            }

            bool isGamepadBinding = binding.path.StartsWith("<Gamepad>/", StringComparison.OrdinalIgnoreCase);

            if (gamepad != isGamepadBinding)
            {
                continue;
            }

            if (!gamepad && !binding.path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) && !binding.path.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string path = binding.effectivePath;

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            switch (path.ToLowerInvariant())
            {
                case "<gamepad>/lefttrigger": return "LT";
                case "<gamepad>/righttrigger": return "RT";
                case "<gamepad>/leftshoulder": return "LB";
                case "<gamepad>/rightshoulder": return "RB";
                case "<gamepad>/buttonwest": return "X";
                case "<gamepad>/buttonnorth": return "Y";

                case "<mouse>/leftbutton": return "LMB";
                case "<mouse>/rightbutton": return "RMB";
                case "<mouse>/middlebutton": return "MMB";
            }

            string display = action.GetBindingDisplayString(i);

            if (!string.IsNullOrEmpty(display))
            {
                return display;
            }
        }

        return fallback;
    }

    private string GetMoveBindingText()
    {
        if (currentActions == null)
        {
            return "WASD / 방향키";
        }

        InputAction action = currentActions.FindAction("Player/Move", false);

        if (action == null)
        {
            return "WASD / 방향키";
        }

        string up = null;
        string left = null;
        string down = null;
        string right = null;

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (binding.isComposite)
            {
                up = null;
                left = null;
                down = null;
                right = null;
                continue;
            }

            if (!binding.isPartOfComposite || string.IsNullOrEmpty(binding.effectivePath) || !binding.effectivePath.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string display = action.GetBindingDisplayString(i);

            switch (binding.name.ToLowerInvariant())
            {
                case "up":
                    up = display;
                    break;

                case "left":
                    left = display;
                    break;

                case "down":
                    down = display;
                    break;

                case "right":
                    right = display;
                    break;
            }

            if (up != null && left != null && down != null && right != null)
            {
                if (up.Length == 1 && left.Length == 1 && down.Length == 1 && right.Length == 1)
                {
                    return $"{up}{left}{down}{right}";
                }

                return $"{up}/{left}/{down}/{right}";
            }
        }

        return "WASD / 방향키";
    }

    private void OnDestroy()
    {
        if (keyboardBtn != null)
        {
            keyboardBtn.onClick.RemoveListener(ShowKeyboard);
        }

        if (gamepadBtn != null)
        {
            gamepadBtn.onClick.RemoveListener(ShowGamepad);
        }

        if (previewActions != null)
        {
            Destroy(previewActions);
        }
    }
}