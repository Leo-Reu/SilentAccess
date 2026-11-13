using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(EventSystem))]
public class UIInputSelectionController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private float mouseMoveThreshold = 3f;
    [SerializeField] private float stickThreshold = 0.6f;

    [Header("Optional Default Selection")]
    [SerializeField] private Selectable[] defaultSelections;

    private EventSystem eventSystem;
    private GameObject lastSelected;
    private bool isGamepadMode;
    private bool wasNavigateHeld;

    private void Awake()
    {
        eventSystem = GetComponent<EventSystem>();
    }

    private void LateUpdate()
    {
        if (eventSystem == null)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        bool mouseUsed = mouse != null && (mouse.delta.ReadValue().sqrMagnitude > mouseMoveThreshold * mouseMoveThreshold || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.scroll.ReadValue().sqrMagnitude > 0f);

        bool keyboardNavigate = keyboard != null && (keyboard.wKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame);

        bool navigateHeld = gamepad != null && (gamepad.dpad.ReadValue().sqrMagnitude > 0.25f || gamepad.leftStick.ReadValue().sqrMagnitude > stickThreshold * stickThreshold);
        bool navigateStarted = navigateHeld && !wasNavigateHeld;
        wasNavigateHeld = navigateHeld;

        bool gamepadButtonPressed = gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame || gamepad.selectButton.wasPressedThisFrame || gamepad.leftShoulder.wasPressedThisFrame || gamepad.rightShoulder.wasPressedThisFrame);

        if (mouseUsed)
        {
            isGamepadMode = false;
        }
        else if (navigateStarted || gamepadButtonPressed || keyboardNavigate)
        {
            isGamepadMode = true;
        }

        GameObject selected = eventSystem.currentSelectedGameObject;

        if (selected != null && IsValidSelection(selected))
        {
            lastSelected = selected;
        }

        if (!isGamepadMode)
        {
            if (selected != null && !IsDropdownExpanded())
            {
                eventSystem.SetSelectedGameObject(null);
            }

            return;
        }

        if (selected != null)
        {
            return;
        }

        if (navigateStarted || gamepadButtonPressed || keyboardNavigate)
        {
            RestoreSelection();
        }
    }

    private void RestoreSelection()
    {
        if (IsValidSelection(lastSelected))
        {
            eventSystem.SetSelectedGameObject(lastSelected);
            return;
        }

        if (defaultSelections != null)
        {
            foreach (Selectable selectable in defaultSelections)
            {
                if (IsValidSelectable(selectable))
                {
                    eventSystem.SetSelectedGameObject(selectable.gameObject);
                    return;
                }
            }
        }

        foreach (Selectable selectable in Selectable.allSelectablesArray)
        {
            if (IsValidSelectable(selectable))
            {
                eventSystem.SetSelectedGameObject(selectable.gameObject);
                return;
            }
        }
    }

    private bool IsValidSelection(GameObject selected)
    {
        if (selected == null)
        {
            return false;
        }

        return IsValidSelectable(selected.GetComponent<Selectable>());
    }

    private bool IsValidSelectable(Selectable selectable)
    {
        return selectable != null && selectable.IsActive() && selectable.IsInteractable();
    }

    private bool IsDropdownExpanded()
    {
        TMP_Dropdown[] dropdowns = FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (TMP_Dropdown dropdown in dropdowns)
        {
            if (dropdown != null && dropdown.IsExpanded)
            {
                return true;
            }
        }

        return false;
    }
}