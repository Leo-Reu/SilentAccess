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
    }

    private void UpdateUI()
    {
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
            statusText.text = "Server Door Not Assigned";
        }
        else if (serverDoor.IsUnlocked)
        {
            statusText.text = "Server Unlocked";
        }
        else if (currentInventory == null || !currentInventory.HasKeycard)
        {
            statusText.text = "Keycard Required";
        }
        else
        {
            statusText.text = "Server Locked";
        }
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
        currentInventory = null;
    }

    private void OnDisable()
    {
        CloseTerminal();
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
