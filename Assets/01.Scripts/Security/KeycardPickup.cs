using UnityEngine;

public class KeycardPickup : MonoBehaviour, IInteractable
{
    public void Interact(PlayerInteraction player)
    {
        if (player == null)
        {
            return;
        }

        PlayerInventory inventory = player.GetComponent<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogWarning("PlayerInventory가 없습니다.");
            return;
        }

        if (inventory.CollectKeycard())
        {
            gameObject.SetActive(false);
        }
    }
}