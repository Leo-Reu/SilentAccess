using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainServer : MonoBehaviour, IInteractable
{
    [Header("Download")]
    [SerializeField] private float downloadDuration = 3f;

    [Header("UI")]
    [SerializeField] private GameObject serverPanel;
    [SerializeField] private Button startDownloadBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private TextMeshProUGUI statusText;

    private PlayerInteraction currentPlayer;
    private float downloadTimer;
    private bool isDownloading;
    private bool isDataStolen;

    public bool IsDataStolen => isDataStolen;
    public event Action OnDataStolen;

    private void Awake()
    {
        if (serverPanel != null)
        {
            serverPanel.SetActive(false);
        }

        if (startDownloadBtn != null)
        {
            startDownloadBtn.onClick.AddListener(StartDownload);
        }

        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(ClosePanel);
        }
    }

    private void Update()
    {
        if (currentPlayer != null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePanel();
            return;
        }

        if (!isDownloading)
        {
            return;
        }

        downloadTimer += Time.deltaTime;

        if (downloadTimer >= Mathf.Max(0.1f, downloadDuration))
        {
            CompleteDownload();
            return;
        }

        UpdateUI();
    }

    public void Interact(PlayerInteraction player)
    {
        if (player == null || currentPlayer != null || serverPanel == null)
        {
            return;
        }

        currentPlayer = player;
        currentPlayer.SetControlEnabled(false);

        serverPanel.SetActive(true);
        UpdateUI();
    }

    private void StartDownload()
    {
        if (currentPlayer == null || isDownloading || isDataStolen)
        {
            return;
        }

        downloadTimer = 0f;
        isDownloading = true;

        UpdateUI();
        Debug.Log("[MainServer] Download started");
    }

    private void CompleteDownload()
    {
        isDownloading = false;
        isDataStolen = true;

        UpdateUI();

        Debug.Log("[MainServer] Data theft completed");
        OnDataStolen?.Invoke();


    }

#if UNITY_EDITOR
    public void DebugCompleteDownload()
    {
        if (isDataStolen)
        {
            return;
        }

        CompleteDownload();
        ClosePanel();
    }
#endif


    public void ClosePanel()
    {
        if (currentPlayer == null)
        {
            return;
        }

        if (isDownloading)
        {
            isDownloading = false;
            downloadTimer = 0f;

            Debug.Log("[MainServer] Download cancelled");
        }

        if (serverPanel != null)
        {
            serverPanel.SetActive(false);
        }

        currentPlayer.SetControlEnabled(true);
        currentPlayer = null;
    }

    private void UpdateUI()
    {
        if (startDownloadBtn != null)
        {
            startDownloadBtn.interactable = currentPlayer != null && !isDownloading && !isDataStolen;
        }

        if (statusText == null)
        {
            return;
        }

        if (isDataStolen)
        {
            statusText.text = "Download Complete";
        }
        else if (isDownloading)
        {
            float progress = Mathf.Clamp01(downloadTimer / Mathf.Max(0.1f, downloadDuration));
            statusText.text = $"Downloading... {Mathf.FloorToInt(progress * 100f)}%";
        }
        else
        {
            statusText.text = "Ready to Download";
        }
    }

    private void OnDisable()
    {
        ClosePanel();
    }

    private void OnDestroy()
    {
        if (startDownloadBtn != null)
        {
            startDownloadBtn.onClick.RemoveListener(StartDownload);
        }

        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveListener(ClosePanel);
        }
    }
}
