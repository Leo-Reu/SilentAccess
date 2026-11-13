using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class ControlsUI : MonoBehaviour
{
    [Header("Windows")]
    [SerializeField] private GameObject settingsWindow;
    [SerializeField] private GameObject controlsWindow;

    [Header("Tabs")]
    [SerializeField] private GameObject keyboardGroup;
    [SerializeField] private GameObject gamepadGroup;

    [Header("Buttons")]
    [SerializeField] private Button controlsBtn;
    [SerializeField] private Button keyboardTabBtn;
    [SerializeField] private Button gamepadTabBtn;
    [SerializeField] private Button resetBtn;
    [SerializeField] private Button backBtn;

    [Header("Navigation")]
    [SerializeField] private Button firstKeyboardButton;
    [SerializeField] private Button firstGamepadButton;
    [SerializeField] private Button lastKeyboardButton;
    [SerializeField] private Button lastGamepadButton;

    [Header("Input Bindings")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Keyboard & Mouse")]
    [SerializeField] private Button moveUpBtn;
    [SerializeField] private Button moveLeftBtn;
    [SerializeField] private Button moveDownBtn;
    [SerializeField] private Button moveRightBtn;
    [SerializeField] private Button keyboardSprintBtn;
    [SerializeField] private Button keyboardCrouchBtn;
    [SerializeField] private Button keyboardInteractBtn;
    [SerializeField] private Button keyboardThrowBtn;

    [Header("Gamepad")]
    [SerializeField] private Button gamepadSprintBtn;
    [SerializeField] private Button gamepadCrouchBtn;
    [SerializeField] private Button gamepadInteractBtn;
    [SerializeField] private Button gamepadThrowBtn;

    private class BindingEntry
    {
        public Button button;
        public InputAction action;
        public int bindingIndex;
        public bool gamepad;
        public bool allowMouse;
        public UnityAction onClick;
    }

    private readonly List<BindingEntry> bindings = new List<BindingEntry>();

    private InputActionAsset runtimeActions;
    private InputActionRebindingExtensions.RebindingOperation rebindOperation;
    private InputSystemUIInputModule uiInputModule;

    private BindingEntry currentBinding;
    private string previousOverridePath;
    private Coroutine noticeCoroutine;
    private bool previousUIModuleEnabled;
    private bool isRebinding;

    public bool IsOpen => controlsWindow != null && controlsWindow.activeInHierarchy;
    public bool IsRebinding => isRebinding;
    public int LastRebindFinishedFrame { get; private set; } = -1;

    private void Awake()
    {
        if (controlsBtn != null) controlsBtn.onClick.AddListener(OpenControls);
        if (keyboardTabBtn != null) keyboardTabBtn.onClick.AddListener(ShowKeyboard);
        if (gamepadTabBtn != null) gamepadTabBtn.onClick.AddListener(ShowGamepad);
        if (resetBtn != null) resetBtn.onClick.AddListener(ResetBindings);
        if (backBtn != null) backBtn.onClick.AddListener(CloseControls);

        if (inputActions != null)
        {
            runtimeActions = Instantiate(inputActions);
            runtimeActions.RemoveAllBindingOverrides();
            InputBindingSave.Load(runtimeActions);
        }

        RegisterBinding(moveUpBtn, "Move", "up", false, false);
        RegisterBinding(moveLeftBtn, "Move", "left", false, false);
        RegisterBinding(moveDownBtn, "Move", "down", false, false);
        RegisterBinding(moveRightBtn, "Move", "right", false, false);

        RegisterBinding(keyboardSprintBtn, "Sprint", null, false, false);
        RegisterBinding(keyboardCrouchBtn, "Crouch", null, false, false);
        RegisterBinding(keyboardInteractBtn, "Interact", null, false, false);
        RegisterBinding(keyboardThrowBtn, "Throw", null, false, true);

        RegisterBinding(gamepadSprintBtn, "Sprint", null, true, false);
        RegisterBinding(gamepadCrouchBtn, "Crouch", null, true, false);
        RegisterBinding(gamepadInteractBtn, "Interact", null, true, false);
        RegisterBinding(gamepadThrowBtn, "Throw", null, true, false);

        RefreshAllBindingTexts();
    }

    private void OnEnable()
    {
        if (settingsWindow != null) settingsWindow.SetActive(true);
        if (controlsWindow != null) controlsWindow.SetActive(false);

        ShowKeyboard();
    }

    private void OnDisable()
    {
        if (isRebinding && rebindOperation != null)
        {
            rebindOperation.Cancel();
        }

        if (noticeCoroutine != null)
        {
            StopCoroutine(noticeCoroutine);
            noticeCoroutine = null;
        }
    }

    private void Update()
    {
        if (!isRebinding || rebindOperation == null)
        {
            return;
        }

        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool gamepadCancelPressed = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;

        if (escapePressed || gamepadCancelPressed)
        {
            rebindOperation.Cancel();
        }
    }

    public void OpenControls()
    {
        if (settingsWindow == null || controlsWindow == null)
        {
            return;
        }

        bool openedWithGamepad = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        RefreshAllBindingTexts();

        settingsWindow.SetActive(false);
        controlsWindow.SetActive(true);

        if (openedWithGamepad)
        {
            ShowGamepad();
            SelectButton(gamepadTabBtn);
        }
        else
        {
            ShowKeyboard();
            ClearSelection();
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayOpen();
    }

    public void CloseControls()
    {
        if (!IsOpen || isRebinding)
        {
            return;
        }

        bool closedWithGamepad = Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame);

        ClearSelection();
        controlsWindow.SetActive(false);
        settingsWindow.SetActive(true);

        if (closedWithGamepad) SelectButton(controlsBtn);

        if (SoundManager.Instance != null) SoundManager.Instance.PlayClose();
    }

    public void ShowKeyboard()
    {
        if (isRebinding)
        {
            return;
        }

        if (keyboardGroup != null) keyboardGroup.SetActive(true);
        if (gamepadGroup != null) gamepadGroup.SetActive(false);

        UpdateNavigation();
    }

    public void ShowGamepad()
    {
        if (isRebinding)
        {
            return;
        }

        if (keyboardGroup != null) keyboardGroup.SetActive(false);
        if (gamepadGroup != null) gamepadGroup.SetActive(true);

        UpdateNavigation();
    }

    private void UpdateNavigation()
    {
        if (keyboardTabBtn == null || gamepadTabBtn == null)
        {
            return;
        }

        bool isKeyboardTab = keyboardGroup != null && keyboardGroup.activeSelf;
        Button firstButton = isKeyboardTab ? firstKeyboardButton : firstGamepadButton;
        Button lastButton = isKeyboardTab ? lastKeyboardButton : lastGamepadButton;

        Navigation keyboardNavigation = keyboardTabBtn.navigation;
        keyboardNavigation.mode = Navigation.Mode.Explicit;
        keyboardNavigation.selectOnUp = null;
        keyboardNavigation.selectOnDown = firstButton;
        keyboardNavigation.selectOnLeft = null;
        keyboardNavigation.selectOnRight = gamepadTabBtn;
        keyboardTabBtn.navigation = keyboardNavigation;

        Navigation gamepadNavigation = gamepadTabBtn.navigation;
        gamepadNavigation.mode = Navigation.Mode.Explicit;
        gamepadNavigation.selectOnUp = null;
        gamepadNavigation.selectOnDown = firstButton;
        gamepadNavigation.selectOnLeft = keyboardTabBtn;
        gamepadNavigation.selectOnRight = null;
        gamepadTabBtn.navigation = gamepadNavigation;

        if (resetBtn != null)
        {
            Navigation resetNavigation = resetBtn.navigation;
            resetNavigation.mode = Navigation.Mode.Explicit;
            resetNavigation.selectOnUp = lastButton;
            resetNavigation.selectOnDown = null;
            resetNavigation.selectOnLeft = null;
            resetNavigation.selectOnRight = backBtn;
            resetBtn.navigation = resetNavigation;
        }

        if (backBtn != null)
        {
            Navigation backNavigation = backBtn.navigation;
            backNavigation.mode = Navigation.Mode.Explicit;
            backNavigation.selectOnUp = lastButton;
            backNavigation.selectOnDown = null;
            backNavigation.selectOnLeft = resetBtn;
            backNavigation.selectOnRight = null;
            backBtn.navigation = backNavigation;
        }
    }

    private void RegisterBinding(Button button, string actionName, string partName, bool gamepad, bool allowMouse)
    {
        if (button == null || runtimeActions == null)
        {
            return;
        }

        InputAction action = runtimeActions.FindAction($"Player/{actionName}", false);

        if (action == null)
        {
            Debug.LogWarning($"키 바인딩 Action을 찾을 수 없습니다: {actionName}");
            return;
        }

        int bindingIndex = FindBindingIndex(action, partName, gamepad);

        if (bindingIndex < 0)
        {
            Debug.LogWarning($"키 바인딩을 찾을 수 없습니다: {actionName} / {partName} / Gamepad={gamepad}");
            return;
        }

        BindingEntry entry = new BindingEntry
        {
            button = button,
            action = action,
            bindingIndex = bindingIndex,
            gamepad = gamepad,
            allowMouse = allowMouse
        };

        entry.onClick = () => BeginRebind(entry);

        button.onClick.AddListener(entry.onClick);
        bindings.Add(entry);
    }

    private int FindBindingIndex(InputAction action, string partName, bool gamepad)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];

            if (binding.isComposite)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(partName))
            {
                if (!binding.isPartOfComposite || !string.Equals(binding.name, partName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }
            else if (binding.isPartOfComposite)
            {
                continue;
            }

            string path = binding.path;

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (gamepad && path.StartsWith("<Gamepad>/", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }

            if (!gamepad && (path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase)))
            {
                return i;
            }
        }

        return -1;
    }

    private void BeginRebind(BindingEntry entry)
    {
        if (runtimeActions == null || entry == null || isRebinding)
        {
            return;
        }

        if (noticeCoroutine != null)
        {
            StopCoroutine(noticeCoroutine);
            noticeCoroutine = null;
        }

        currentBinding = entry;
        previousOverridePath = entry.action.bindings[entry.bindingIndex].overridePath;
        isRebinding = true;

        SetButtonText(entry.button, "?");

        if (EventSystem.current != null)
        {
            uiInputModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
        }

        if (uiInputModule != null)
        {
            previousUIModuleEnabled = uiInputModule.enabled;
            uiInputModule.enabled = false;
        }

        rebindOperation = entry.action.PerformInteractiveRebinding(entry.bindingIndex)
            .WithExpectedControlType("Button")
            .WithCancelingThrough("<Keyboard>/escape")
            .WithControlsExcluding("<Keyboard>/escape")
            .WithControlsExcluding("<Keyboard>/anyKey")
            .WithControlsExcluding("<Gamepad>/buttonSouth")
            .WithControlsExcluding("<Gamepad>/buttonEast")
            .WithControlsExcluding("<Gamepad>/start")
            .WithControlsExcluding("<Gamepad>/select")
            .WithControlsExcluding("<Gamepad>/dpad")
            .WithControlsExcluding("<Gamepad>/leftStick")
            .WithControlsExcluding("<Gamepad>/rightStick")
            .OnComplete(operation => CompleteRebind(operation))
            .OnCancel(operation => CancelRebind(operation));

        if (entry.gamepad)
        {
            rebindOperation.WithControlsHavingToMatchPath("<Gamepad>");
        }
        else if (entry.allowMouse)
        {
            rebindOperation.WithControlsExcluding("<Gamepad>");
        }
        else
        {
            rebindOperation.WithControlsHavingToMatchPath("<Keyboard>");
        }

        rebindOperation.Start();
    }

    private void CompleteRebind(InputActionRebindingExtensions.RebindingOperation operation)
    {
        BindingEntry entry = currentBinding;

        if (entry == null)
        {
            FinishRebind(operation);
            return;
        }

        string newPath = entry.action.bindings[entry.bindingIndex].effectivePath;
        bool supported = IsSupportedPath(entry, newPath);
        bool duplicated = supported && HasDuplicateBinding(entry, newPath);

        if (!supported || duplicated)
        {
            RestorePreviousBinding(entry);
        }
        else
        {
            InputBindingSave.Save(runtimeActions);
            SyncPlayerInput(false);
        }

        FinishRebind(operation);

        if (!supported)
        {
            ShowNotice(entry, "사용 불가");
        }
        else if (duplicated)
        {
            ShowNotice(entry, "중복!");
        }
        else
        {
            RefreshAllBindingTexts();
        }
    }

    private void CancelRebind(InputActionRebindingExtensions.RebindingOperation operation)
    {
        FinishRebind(operation);
        RefreshAllBindingTexts();
    }

    private void FinishRebind(InputActionRebindingExtensions.RebindingOperation operation)
    {
        operation.Dispose();
        rebindOperation = null;
        currentBinding = null;
        previousOverridePath = null;

        isRebinding = false;
        LastRebindFinishedFrame = Time.frameCount;

        if (uiInputModule != null)
        {
            uiInputModule.enabled = previousUIModuleEnabled;
            uiInputModule = null;
        }
    }

    private bool IsSupportedPath(BindingEntry entry, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (!entry.gamepad)
        {
            if (path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase))
            {
                return !string.Equals(path, "<Keyboard>/escape", StringComparison.OrdinalIgnoreCase) && !string.Equals(path, "<Keyboard>/anyKey", StringComparison.OrdinalIgnoreCase);
            }

            return entry.allowMouse && path.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(path, "<Gamepad>/leftTrigger", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "<Gamepad>/rightTrigger", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "<Gamepad>/leftShoulder", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "<Gamepad>/rightShoulder", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "<Gamepad>/buttonWest", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "<Gamepad>/buttonNorth", StringComparison.OrdinalIgnoreCase);
    }

    private bool HasDuplicateBinding(BindingEntry entry, string newPath)
    {
        InputActionMap playerMap = runtimeActions.FindActionMap("Player", false);

        if (playerMap == null)
        {
            return false;
        }

        foreach (InputAction action in playerMap.actions)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];

                if (binding.isComposite)
                {
                    continue;
                }

                if (action == entry.action && i == entry.bindingIndex)
                {
                    continue;
                }

                if (string.Equals(binding.effectivePath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void RestorePreviousBinding(BindingEntry entry)
    {
        if (string.IsNullOrEmpty(previousOverridePath))
        {
            entry.action.RemoveBindingOverride(entry.bindingIndex);
        }
        else
        {
            entry.action.ApplyBindingOverride(entry.bindingIndex, previousOverridePath);
        }
    }

    private void ResetBindings()
    {
        if (runtimeActions == null || isRebinding)
        {
            return;
        }

        if (noticeCoroutine != null)
        {
            StopCoroutine(noticeCoroutine);
            noticeCoroutine = null;
        }

        runtimeActions.RemoveAllBindingOverrides();
        InputBindingSave.Reset();

        SyncPlayerInput(true);
        RefreshAllBindingTexts();
    }

    private void SyncPlayerInput(bool reset)
    {
        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();

        if (playerInput == null)
        {
            return;
        }

        playerInput.actions.RemoveAllBindingOverrides();

        if (!reset)
        {
            InputBindingSave.Load(playerInput.actions);
        }
    }

    private void RefreshAllBindingTexts()
    {
        foreach (BindingEntry entry in bindings)
        {
            if (entry == null || entry.button == null || entry.action == null)
            {
                continue;
            }

            string path = entry.action.bindings[entry.bindingIndex].effectivePath;
            string display = GetBindingText(entry.action, entry.bindingIndex, path);

            SetButtonText(entry.button, display);
        }
    }

    private string GetBindingText(InputAction action, int bindingIndex, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "-";
        }

        switch (path.ToLowerInvariant())
        {
            case "<gamepad>/lefttrigger": return "LT";
            case "<gamepad>/righttrigger": return "RT";
            case "<gamepad>/leftshoulder": return "LB";
            case "<gamepad>/rightshoulder": return "RB";
            case "<gamepad>/buttonwest": return "X";
            case "<gamepad>/buttonnorth": return "Y";

            case "<mouse>/leftbutton": return "마우스 좌클릭";
            case "<mouse>/rightbutton": return "마우스 우클릭";
            case "<mouse>/middlebutton": return "마우스 휠클릭";
        }

        return action.GetBindingDisplayString(bindingIndex);
    }

    private void SetButtonText(Button button, string value)
    {
        if (button == null)
        {
            return;
        }

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);

        if (text != null)
        {
            text.text = value;
        }
    }

    private void ShowNotice(BindingEntry entry, string message)
    {
        if (entry == null || entry.button == null)
        {
            return;
        }

        noticeCoroutine = StartCoroutine(ShowNoticeRoutine(entry.button, message));
    }

    private IEnumerator ShowNoticeRoutine(Button button, string message)
    {
        SetButtonText(button, message);

        yield return new WaitForSecondsRealtime(1.2f);

        noticeCoroutine = null;

        if (!isRebinding)
        {
            RefreshAllBindingTexts();
        }
    }

    private void SelectButton(Button button)
    {
        if (EventSystem.current == null || button == null || !button.IsActive() || !button.IsInteractable())
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void ClearSelection()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnDestroy()
    {
        if (controlsBtn != null) controlsBtn.onClick.RemoveListener(OpenControls);
        if (keyboardTabBtn != null) keyboardTabBtn.onClick.RemoveListener(ShowKeyboard);
        if (gamepadTabBtn != null) gamepadTabBtn.onClick.RemoveListener(ShowGamepad);
        if (resetBtn != null) resetBtn.onClick.RemoveListener(ResetBindings);
        if (backBtn != null) backBtn.onClick.RemoveListener(CloseControls);

        foreach (BindingEntry entry in bindings)
        {
            if (entry.button != null && entry.onClick != null)
            {
                entry.button.onClick.RemoveListener(entry.onClick);
            }
        }

        if (rebindOperation != null) rebindOperation.Dispose();
        if (uiInputModule != null) uiInputModule.enabled = previousUIModuleEnabled;
        if (runtimeActions != null) Destroy(runtimeActions);
    }
}