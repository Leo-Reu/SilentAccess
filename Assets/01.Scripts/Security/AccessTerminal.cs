using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AccessTerminal : MonoBehaviour, IInteractable
{
    [Header("Door")]
    [SerializeField] private SecurityDoor serverDoor;

    [Header("UI")]
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private Button unlockServerBtn;
    [SerializeField] private Button closeBtn;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Status Icons")]
    [SerializeField] private GameObject lockedImage;
    [SerializeField] private GameObject unlockedIndicator;

    private PlayerInteraction currentPlayer;
    private PlayerInventory currentInventory;

    private void Awake()
    {
        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }

        if (unlockServerBtn != null)
        {
            unlockServerBtn.onClick.AddListener(UnlockServer);
        }

        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(CloseTerminal);
        }
    }

    private void Update()
    {
        if (currentPlayer != null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseTerminal();
        }
    }

    public void Interact(PlayerInteraction player)
    {
        if (currentPlayer != null || player == null || terminalPanel == null)
        {
            return;
        }

        currentPlayer = player;
        currentInventory = player.GetComponentInParent<PlayerInventory>();

        currentPlayer.SetControlEnabled(false);
        terminalPanel.SetActive(true);

        UpdateUI();

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOpen();
        }
    }

    private void UnlockServer()
    {
        if (currentPlayer == null || currentInventory == null || serverDoor == null)
        {
            return;
        }

        if (!currentInventory.HasKeycard || serverDoor.IsUnlocked)
        {
            return;
        }

        serverDoor.Unlock();
        UpdateUI();

        SoundManager.Instance?.PlayConfirm();
    }

    private void UpdateUI()
    {
        bool isUnlocked = serverDoor != null && serverDoor.IsUnlocked;

        if (lockedImage != null)
        {
            lockedImage.SetActive(!isUnlocked);
        }

        if (unlockedIndicator != null)
        {
            unlockedIndicator.SetActive(isUnlocked);
        }

        if (statusText != null)
        {
            statusText.color = isUnlocked ? new Color32(100, 226, 179, 255) : Color.white;
        }

        bool canUnlock = currentInventory != null && currentInventory.HasKeycard && serverDoor != null && !serverDoor.IsUnlocked;

        if (unlockServerBtn != null)
        {
            unlockServerBtn.interactable = canUnlock;
        }

        if (statusText == null)
        {
            return;
        }

        if (serverDoor == null)
        {
            statusText.text = "서버실 출입문 연결 오류";
        }
        else if (serverDoor.IsUnlocked)
        {
            statusText.text = "서버실 잠금 해제 완료";
        }
        else if (currentInventory == null || !currentInventory.HasKeycard)
        {
            statusText.text = "카드키가 필요합니다";
        }
        else
        {
            statusText.text = "서버실 잠김";
        }
    }

    public void CloseTerminal()
    {
        if (currentPlayer == null)
        {
            return;
        }

        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }

        currentPlayer.SetControlEnabled(true);
        currentPlayer = null;
        currentInventory = null;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayClose();
        }
    }

    private void OnDisable()
    {
        // 오브젝트 비활성화 시 플레이어 조작을 복구하지 않음
        if (terminalPanel != null)
        {
            terminalPanel.SetActive(false);
        }

        currentPlayer = null;
        currentInventory = null;
    }

    private void OnDestroy()
    {
        if (unlockServerBtn != null)
        {
            unlockServerBtn.onClick.RemoveListener(UnlockServer);
        }

        if (closeBtn != null)
        {
            closeBtn.onClick.RemoveListener(CloseTerminal);
        }
    }
}
