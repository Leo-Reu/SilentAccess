using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SecurityTerminal : MonoBehaviour, IInteractable
{
    [Header("CCTV")]
    [SerializeField] private CCTVController[] cctvs;
    [SerializeField] private float disableDuration = 15f;

    [Header("UI")]
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private Button disableCCTVBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private TextMeshProUGUI statusText;

    private PlayerInteraction currentPlayer;

    private float remainTime;
    private bool isCCTVDisabled;

    private void Awake()
    {
        terminalPanel.SetActive(false);

        disableCCTVBtn.onClick.AddListener(DisableCCTV);
        closeBtn.onClick.AddListener(CloseTerminal);
    }

    private void Update()
    {
        if (currentPlayer != null &&
            Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
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
        if (currentPlayer != null || player == null)
        {
            return;
        }

        currentPlayer = player;
        currentPlayer.SetControlEnabled(false);

        terminalPanel.SetActive(true);

        UpdateUI();
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

        UpdateUI();
    }

    public void CloseTerminal()
    {
        if (currentPlayer == null)
        {
            return;
        }

        terminalPanel.SetActive(false);

        currentPlayer.SetControlEnabled(true);
        currentPlayer = null;
    }

    private void UpdateUI()
    {
        if (disableCCTVBtn != null)
        {
            disableCCTVBtn.interactable = !isCCTVDisabled;
        }

        if (statusText == null)
        {
            return;
        }

        statusText.text = isCCTVDisabled
            ? $"CCTV OFF ({Mathf.CeilToInt(remainTime)}s)"
            : "CCTV ON";
    }

    private void OnDisable()
    {
        CloseTerminal();

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