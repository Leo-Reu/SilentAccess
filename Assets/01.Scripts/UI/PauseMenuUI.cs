using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteraction player;
    [SerializeField] private MissionController missionController;

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private Button resumeButton;

    [Header("Scene")]
    [SerializeField] private string mainSceneName = "MainScene";

    [SerializeField] private SettingsUI settingsUI;

    private InputAction pauseAction;

    private void Awake()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }

        if (player != null)
        {
            PlayerInput playerInput = player.GetComponent<PlayerInput>();

            if (playerInput != null)
            {
                pauseAction = playerInput.actions["Pause"];
            }
        }
    }

    private void OnEnable()
    {
        if (missionController != null)
        {
            missionController.OnMissionEnded += HandleMissionEnded;
        }
    }

    private void OnDisable()
    {
        if (missionController != null)
        {
            missionController.OnMissionEnded -= HandleMissionEnded;
        }
    }

    private void Update()
    {
        if (settingsUI != null && (settingsUI.IsOpen || settingsUI.LastClosedFrame == Time.frameCount))
        {
            return;
        }

        bool pausePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool gamepadPausePressed = pauseAction != null && pauseAction.WasPressedThisFrame();
        bool gamepadCancelPressed = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;

        if (howToPlayPanel != null && howToPlayPanel.activeSelf)
        {
            if (pausePressed || gamepadPausePressed || gamepadCancelPressed)
            {
                CloseHowToPlay();
            }

            return;
        }

        if (missionController != null && missionController.CurrentResult != MissionResult.None)
        {
            return;
        }

        if (!pausePressed && !gamepadPausePressed)
        {
            return;
        }

        if (pausePanel != null && pausePanel.activeSelf)
        {
            ResumeGame();
            return;
        }

        if (player == null || missionController == null)
        {
            return;
        }

        if (!player.CanInteract)
        {
            return;
        }

        if (player.LastControlEnabledFrame == Time.frameCount)
        {
            return;
        }

        PauseGame();
    }

    public void PauseGame()
    {
        if (pausePanel == null || player == null || missionController == null)
        {
            return;
        }

        if (pausePanel.activeSelf || missionController.CurrentResult != MissionResult.None || !player.CanInteract)
        {
            return;
        }

        ClearSelectedButton();

        player.SetControlEnabled(false);
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        SelectResumeButton();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOpen();
        }
    }

    public void OpenHowToPlay()
    {
        if (pausePanel == null || howToPlayPanel == null || !pausePanel.activeSelf)
        {
            return;
        }

        ClearSelectedButton();
        pausePanel.SetActive(false);
        howToPlayPanel.SetActive(true);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOpen();
        }
    }

    public void CloseHowToPlay()
    {
        if (pausePanel == null || howToPlayPanel == null || !howToPlayPanel.activeSelf)
        {
            return;
        }

        ClearSelectedButton();
        howToPlayPanel.SetActive(false);
        pausePanel.SetActive(true);
        SelectResumeButton();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClose();
        }
    }

    public void ResumeGame()
    {
        if (pausePanel == null || player == null)
        {
            return;
        }

        ClearSelectedButton();
        pausePanel.SetActive(false);

        // 미션 종료 후에는 게임이 재개되지 않도록 방지
        if (missionController != null && missionController.CurrentResult != MissionResult.None)
        {
            player.SetControlEnabled(false);
            Time.timeScale = 0f;
            return;
        }

        Time.timeScale = 1f;
        player.SetControlEnabled(true);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClose();
        }
    }

    public void ReturnToMainMenu()
    {
        ClearSelectedButton();

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneName);
    }

    private void HandleMissionEnded(MissionResult result)
    {
        if (settingsUI != null && settingsUI.IsOpen)
        {
            settingsUI.CloseWithoutReturn();
        }

        if (result == MissionResult.None)
        {
            return;
        }

        // 미션 종료 시 PausePanel을 닫고 게임 정지 상태 유지
        ClearSelectedButton();

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (howToPlayPanel != null)
        {
            howToPlayPanel.SetActive(false);
        }

        if (player != null)
        {
            player.SetControlEnabled(false);
        }

        Time.timeScale = 0f;
    }
    private void SelectResumeButton()
    {
        if (resumeButton == null || EventSystem.current == null || !resumeButton.IsActive() || !resumeButton.IsInteractable())
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    private void ClearSelectedButton()
    {
        // 이전에 클릭한 버튼의 Selected 상태 해제
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
