using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteraction player;
    [SerializeField] private MissionController missionController;

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("Scene")]
    [SerializeField] private string mainSceneName = "MainScene";

    [SerializeField] private SettingsUI settingsUI;

    private void Awake()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
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

        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        // 미션 종료 상태에서는 ESC 입력 무시
        if (missionController != null && missionController.CurrentResult != MissionResult.None)
        {
            return;
        }

        // 일시 정지 중이면 게임 재개
        if (pausePanel != null && pausePanel.activeSelf)
        {
            ResumeGame();
            return;
        }

        if (player == null || missionController == null)
        {
            return;
        }

        // 다른 UI를 조작 중이면 일시 정지하지 않음
        if (!player.CanInteract)
        {
            return;
        }

        // 터미널을 ESC로 닫은 프레임에는 Pause가 열리지 않도록 방지
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

        if (player != null)
        {
            player.SetControlEnabled(false);
        }

        Time.timeScale = 0f;
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
