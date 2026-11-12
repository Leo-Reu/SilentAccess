using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SecurityTerminal : MonoBehaviour, IInteractable
{
    [Header("CCTV")]
    [SerializeField] private CCTVController[] cctvs;
    [SerializeField] private float disableDuration = 10f;

    [Header("UI")]
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private Button disableCCTVBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Status Icons")]
    [SerializeField] private GameObject cctvActiveImage;
    [SerializeField] private GameObject cctvDisabledIndicator;

    private PlayerInteraction currentPlayer;

    private float remainTime;
    private bool isCCTVDisabled;

    private void Awake()
    {
        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }

        if (disableCCTVBtn != null)
        {
            disableCCTVBtn.onClick.AddListener(DisableCCTV);
        }

        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(CloseTerminal);
        }
    }

    private void Update()
    {
        bool cancelPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool gamepadCancelPressed = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;

        if (currentPlayer != null && (cancelPressed || gamepadCancelPressed))
        {
            CloseTerminal();
        }

        if (!isCCTVDisabled)
        {
            return;
        }

        remainTime -= Time.deltaTime;

        if (remainTime <= 0f)
        {
            RestoreCCTV();
            return;
        }

        if (currentPlayer != null)
        {
            UpdateUI();
        }
    }

    public void Interact(PlayerInteraction player)
    {
        if (currentPlayer != null || player == null || terminalPanel == null)
        {
            return;
        }

        currentPlayer = player;
        currentPlayer.SetControlEnabled(false);

        terminalPanel.SetActive(true);

        UpdateUI();
        SelectButton(disableCCTVBtn != null && disableCCTVBtn.IsInteractable() ? disableCCTVBtn : closeBtn);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOpen();
        }
    }

    private void DisableCCTV()
    {
        if (currentPlayer == null || isCCTVDisabled)
        {
            return;
        }

        bool hasCCTV = false;

        foreach (CCTVController cctv in cctvs)
        {
            if (cctv == null)
            {
                continue;
            }

            cctv.SetDetectionEnabled(false);
            hasCCTV = true;
        }

        if (!hasCCTV)
        {
            if (statusText != null)
            {
                statusText.text = "연결된 CCTV 없음";
            }

            return;
        }

        isCCTVDisabled = true;
        remainTime = Mathf.Max(0.1f, disableDuration);

        UpdateUI();
        SelectButton(closeBtn);

        SoundManager.Instance?.PlayConfirm();
    }

    private void RestoreCCTV()
    {
        foreach (CCTVController cctv in cctvs)
        {
            if (cctv != null)
            {
                cctv.SetDetectionEnabled(true);
            }
        }

        isCCTVDisabled = false;
        remainTime = 0f;

        if (isActiveAndEnabled)
        {
            UpdateUI();
        }
    }

    public void CloseTerminal()
    {
        if (currentPlayer == null)
        {
            return;
        }

        ClearSelectedButton();

        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }

        currentPlayer.SetControlEnabled(true);
        currentPlayer = null;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClose();
        }
    }

    private void UpdateUI()
    {
        if (cctvActiveImage != null)
        {
            cctvActiveImage.SetActive(!isCCTVDisabled);
        }

        if (cctvDisabledIndicator != null)
        {
            cctvDisabledIndicator.SetActive(isCCTVDisabled);
        }

        if (disableCCTVBtn != null)
        {
            disableCCTVBtn.interactable = !isCCTVDisabled;
        }

        if (statusText == null)
        {
            return;
        }

        statusText.color = isCCTVDisabled ? new Color32(100, 226, 179, 255) : Color.white;
        statusText.text = isCCTVDisabled ? $"CCTV 비활성화 ({Mathf.CeilToInt(remainTime)}초)" : "CCTV 작동 중";
    }

    private void SelectButton(Button button)
    {
        if (button == null || EventSystem.current == null || !button.IsActive() || !button.IsInteractable())
        {
            return;
        }

        ClearSelectedButton();
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void ClearSelectedButton()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnDisable()
    {
        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }

        currentPlayer = null;

        if (isCCTVDisabled)
        {
            RestoreCCTV();
        }
    }

    private void OnDestroy()
    {
        if (disableCCTVBtn != null)
        {
            disableCCTVBtn.onClick.RemoveListener(DisableCCTV);
        }

        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveListener(CloseTerminal);
        }
    }
}