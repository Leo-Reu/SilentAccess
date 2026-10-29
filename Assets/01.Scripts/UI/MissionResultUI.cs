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

    [Header("Result Icons")]
    [SerializeField] private GameObject failImage;
    [SerializeField] private GameObject successImage;

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

        if (failImage != null)
        {
            failImage.SetActive(!isSuccess);
        }

        if (successImage != null)
        {
            successImage.SetActive(isSuccess);
        }

        if (resultText != null)
        {
            resultText.color = isSuccess ? new Color32(100, 226, 179, 255) : new Color32(250, 114, 121, 255);
        }

        if (resultText != null)
        {
            resultText.text = isSuccess ? "미션 성공" : "미션 실패";
        }

        if (retryBtnText != null)
        {
            retryBtnText.text = isSuccess ? "처음부터 플레이" : "다시 시도";
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
