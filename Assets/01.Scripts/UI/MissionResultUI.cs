using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionResultUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MissionController missionController;
    [SerializeField] private PlayerInteraction player;

    [Header("UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button retryBtn;
    [SerializeField] private TextMeshProUGUI retryBtnText;

    private void Awake()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (retryBtn != null)
        {
            retryBtn.onClick.AddListener(Retry);
        }
    }

    private void OnEnable()
    {
        if (missionController != null)
        {
            missionController.OnMissionEnded += ShowResult;
        }
    }

    private void OnDisable()
    {
        if (missionController != null)
        {
            missionController.OnMissionEnded -= ShowResult;
        }
    }

    private void ShowResult(MissionResult result)
    {
        if (resultPanel == null || result == MissionResult.None)
        {
            return;
        }

        bool isSuccess = result == MissionResult.Success;

        if (resultText != null)
        {
            resultText.text = isSuccess ? "MISSION COMPLETE" : "MISSION FAILED";
        }

        if (retryBtnText != null)
        {
            retryBtnText.text = isSuccess ? "PLAY AGAIN" : "RETRY";
        }

        resultPanel.SetActive(true);

        if (player != null)
        {
            player.SetControlEnabled(false);
        }

        Time.timeScale = 0f;
    }

    private void Retry()
    {
        if (missionController != null)
        {
            missionController.RetryMission();
        }
    }

    private void OnDestroy()
    {
        if (retryBtn != null)
        {
            retryBtn.onClick.RemoveListener(Retry);
        }
    }
}
