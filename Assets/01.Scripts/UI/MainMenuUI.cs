using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("UI")]
    [SerializeField] private GameObject menuButtons;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("Navigation")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button howToPlayCloseButton;

    private InputAction uiCancelAction;

    private void Awake()
    {
        // 메인 메뉴 진입 시 시간과 마우스 상태 초기화
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }

        SoundManager.Instance?.PlayBGM(BGMType.MainMenu);
    }
    private void Start()
    {
        if (EventSystem.current != null)
        {
            InputSystemUIInputModule uiModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();

            if (uiModule != null && uiModule.cancel != null)
            {
                uiCancelAction = uiModule.cancel.action;
            }
        }

        SelectButton(startButton);
    }

    private void Update()
    {
        bool cancelPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool uiCancelPressed = uiCancelAction != null && uiCancelAction.WasPressedThisFrame();

        if (howToPlayPanel != null && howToPlayPanel.activeSelf && (cancelPressed || uiCancelPressed))
        {
            CloseHowToPlay();
        }

        if (menuButtons != null && menuButtons.activeInHierarchy && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && Gamepad.current != null && (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.25f || Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.25f))
        {
            SelectButton(startButton);
        }
    }

    public void OpenHowToPlay()
    {
        if (howToPlayPanel == null || howToPlayPanel.activeSelf)
        {
            return;
        }

        if (menuButtons != null)
        {
            menuButtons.SetActive(false);
        }

        howToPlayPanel.SetActive(true);
        SelectButton(howToPlayCloseButton);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOpen();
        }
    }

    public void CloseHowToPlay()
    {
        if (howToPlayPanel == null || !howToPlayPanel.activeSelf)
        {
            return;
        }

        howToPlayPanel.SetActive(false);

        if (menuButtons != null)
        {
            menuButtons.SetActive(true);
        }

        SelectButton(howToPlayButton);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClose();
        }
    }

    public void StartGame()
    {
        // 게임 씬으로 전환
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }
    private void SelectButton(Button button)
    {
        if (button == null || EventSystem.current == null || !button.IsActive() || !button.IsInteractable())
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    public void QuitGame()
    {
        // 에디터에서는 플레이 모드 종료
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
