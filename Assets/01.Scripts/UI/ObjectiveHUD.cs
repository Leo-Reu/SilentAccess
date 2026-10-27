using TMPro;
using UnityEngine;

public class ObjectiveHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private SecurityDoor serverDoor;
    [SerializeField] private MissionController missionController;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private void OnEnable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnKeycardCollected += RefreshObjective;
        }

        if (serverDoor != null)
        {
            serverDoor.OnUnlocked += RefreshObjective;
        }

        if (missionController != null)
        {
            missionController.OnPhaseChanged += HandlePhaseChanged;
            missionController.OnMissionEnded += HandleMissionEnded;
        }

        RefreshObjective();
    }

    private void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnKeycardCollected -= RefreshObjective;
        }

        if (serverDoor != null)
        {
            serverDoor.OnUnlocked -= RefreshObjective;
        }

        if (missionController != null)
        {
            missionController.OnPhaseChanged -= HandlePhaseChanged;
            missionController.OnMissionEnded -= HandleMissionEnded;
        }
    }

    private void RefreshObjective()
    {
        if (playerInventory == null || serverDoor == null || missionController == null || titleText == null || descriptionText == null)
        {
            return;
        }

        if (missionController.CurrentResult != MissionResult.None)
        {
            gameObject.SetActive(false);
            return;
        }

        if (missionController.CurrentPhase == MissionPhase.Escape)
        {
            SetObjective("시설 탈출", "로비의 탈출 지점으로 이동하세요.");
        }
        else if (serverDoor.IsUnlocked)
        {
            SetObjective("데이터 탈취", "서버실에서 데이터를 다운로드하세요.");
        }
        else if (playerInventory.HasKeycard)
        {
            SetObjective("보안실 접근", "터미널에서 서버실 잠금을 해제하세요.");
        }
        else
        {
            SetObjective("카드키 획득", "시설 내부에서 보안 카드키를 찾으세요.");
        }
    }

    private void SetObjective(string title, string description)
    {
        titleText.text = title;
        descriptionText.text = description;
    }

    private void HandlePhaseChanged(MissionPhase phase)
    {
        RefreshObjective();
    }

    private void HandleMissionEnded(MissionResult result)
    {
        gameObject.SetActive(false);
    }
}